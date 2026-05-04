<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <div class="content-grid__back">
        <router-link
          :to="{ name: 'classes.detail', params: { classId } }"
          class="content-grid__back-link"
          aria-label="Retour"
        >
          &lt;
        </router-link>
        <h1>{{ className && groupName ? `${className} - ${groupName}` : groupName || t('navigation.groups') }}</h1>
      </div>
      <div class="content-grid__actions">
        <button type="button" class="btn" @click="showCreateExamPopup = true">
          {{ t('navigation.addExam') }}
        </button>
      </div>
    </div>

    <Card>
      <div v-if="loading" class="groups-empty">{{ t('global.loading') }}</div>
      <div v-else-if="exams.length === 0" class="groups-empty">
        {{ t('navigation.noExams') }}
      </div>
      <ul v-else class="groups-list">
        <li v-for="exam in exams" :key="exam.id" class="groups-list__item">
          <router-link
            :to="{ name: 'classes.examDetail', params: { classId, groupId, examId: exam.id } }"
            class="groups-list__link"
          >
            <span class="groups-list__icon">&#128196;</span>
            <div class="groups-list__info">
              <span class="groups-list__name">{{ exam.name }}</span>
            </div>
            <span class="groups-list__arrow">&#8250;</span>
          </router-link>
          <button
            type="button"
            class="groups-list__export"
            title="Export PDF"
            @click="onExportPdf(exam)"
          >
            PDF
          </button>
          <button
            type="button"
            class="groups-list__delete"
            :title="t('navigation.deleteExamConfirm')"
            @click="onDeleteExam(exam)"
          >
            ✕
          </button>
        </li>
      </ul>
    </Card>

    <CreateExamForGroupPopup
      v-if="showCreateExamPopup"
      :class-id="classId"
      :group-id="groupId"
      @close="onExamPopupClose"
    />

    <ConfirmDeletePopup
      v-if="pendingDelete"
      :message="t('navigation.deleteExamConfirm')"
      :is-loading="isDeleting"
      @close="pendingDelete = null"
      @confirm="confirmDelete"
    />

  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue3-i18n';
import { useClassesStore } from '@/stores/classesStore';
import Card from '@/components/layouts/items/Card.vue';
import CreateExamForGroupPopup from '@/components/popups/CreateExamForGroupPopup.vue';
import ConfirmDeletePopup from '@/components/popups/ConfirmDeletePopup.vue';
import { notifySuccess, notifyError } from '@/notify';
import jsPDF from 'jspdf';
import autoTable from 'jspdf-autotable';
import JSZip from 'jszip';
import { saveAs } from 'file-saver';
import {
  type GradeLetter,
  ALL_GRADES,
  GRADE_VALUES,
  numericToLetter,
} from '@/config/gradeConfig';

const { t } = useI18n();
const route = useRoute();
const classesStore = useClassesStore();

const classId = computed(() => route.params.classId as string);
const groupId = computed(() => route.params.groupId as string);
const className = computed(() =>
  classesStore.getClasses.find((c) => c.id === classId.value)?.name ?? ''
);

const exams = ref<{ id: string; name: string }[]>([]);
const groupName = ref('');
const loading = ref(true);
const showCreateExamPopup = ref(false);
const pendingDelete = ref<{ id: string; name: string } | null>(null);
const isDeleting = ref(false);

async function fetchGroupName() {
  const res = await fetch(`/api/classes/${classId.value}/groups`);
  if (!res.ok) return;
  const data = await res.json();
  const group = data.find((g: any) => String(g.id) === groupId.value);
  if (group) groupName.value = group.name;
}

async function fetchExams() {
  loading.value = true;
  try {
    const res = await fetch(`/api/classes/${classId.value}/groups/${groupId.value}/exams`);
    if (!res.ok) return;
    const data = await res.json();
    exams.value = data.map((e: any) => ({ id: String(e.id), name: String(e.name) }));
  } finally {
    loading.value = false;
  }
}

function onDeleteExam(exam: { id: string; name: string }) {
  pendingDelete.value = exam;
}

async function confirmDelete() {
  if (!pendingDelete.value) return;
  isDeleting.value = true;
  try {
    const res = await fetch(`/api/classes/${classId.value}/groups/${groupId.value}/exams/${pendingDelete.value.id}`, {
      method: 'DELETE',
    });
    if (!res.ok) throw new Error();
    pendingDelete.value = null;
    notifySuccess(t('navigation.examDeleted'));
    await fetchExams();
  } catch {
    notifyError(t('navigation.deleteError'));
  } finally {
    isDeleting.value = false;
  }
}

async function onExamPopupClose() {
  showCreateExamPopup.value = false;
  await fetchExams();
}

async function onExportPdf(exam: { id: string; name: string }) {
  const [studentsRes, skillsRes, evalsRes] = await Promise.all([
    fetch(`/api/groups/${groupId.value}/students`),
    fetch(`/api/exams/${exam.id}/skills`),
    fetch(`/api/exams/${exam.id}/evaluations`),
  ]);

  const studentsData = await studentsRes.json();
  const skillsData = await skillsRes.json();
  const evalsData = await evalsRes.json();

  const students = studentsData
    .map((s: any) => ({
      id: String(s.id),
      number: String(s.number),
      firstName: String(s.firstName),
      lastName: String(s.lastName),
    }))
    .sort((a: any, b: any) => a.number.localeCompare(b.number));

  const competencies: {
    id: string;
    name: string;
    criteria: {
      id: string;
      label: string;
      totalValue: number;
      weights: Record<string, number>;
      rawWeights: Record<string, number>;
      descriptions: Record<string, string>;
      options: string[];
    }[];
  }[] = [];

  for (const sk of skillsData) {
    const criteriaRes = await fetch(`/api/exams/${exam.id}/skills/${sk.skillId}/criteria`);
    const criteriaData = await criteriaRes.json();

    const criteria = criteriaData.map((c: any) => {
      const weights: Record<string, number> = {};
      const rawWeights: Record<string, number> = {};
      const descriptions: Record<string, string> = {};
      const options: string[] = [];
      const totalValue = c.totalValue ?? 0;

      for (const w of c.weights ?? []) {
        if (w.isEnabled) {
          options.push(w.weight);
          rawWeights[w.weight] = w.value ?? 0;
          weights[w.weight] = totalValue > 0 ? Math.round((w.value / totalValue) * 100) : 0;
          if (w.description) descriptions[w.weight] = w.description;
        }
      }

      return { id: c.id, label: c.label, totalValue, weights, rawWeights, descriptions, options };
    });

    competencies.push({ id: sk.skillId, name: sk.label, criteria });
  }

  const norm = (id: string) => String(id).toLowerCase();

  const compEvalMap: Record<string, Record<string, { grade: string | null; comment: string }>> = {};
  for (const e of evalsData.competencyEvaluations ?? []) {
    const sid = norm(e.studentId);
    if (!compEvalMap[sid]) compEvalMap[sid] = {};
    compEvalMap[sid][norm(e.competencyId)] = { grade: e.grade || null, comment: e.comment ?? '' };
  }

  const critEvalMap: Record<string, Record<string, { grade: string | null; comment: string }>> = {};
  for (const e of evalsData.criterionEvaluations ?? []) {
    const sid = norm(e.studentId);
    if (!critEvalMap[sid]) critEvalMap[sid] = {};
    critEvalMap[sid][norm(e.criterionId)] = { grade: e.grade || null, comment: e.comment ?? '' };
  }

  const videoUrlMap: Record<string, string> = {};
  for (const v of evalsData.videoUrls ?? []) {
    videoUrlMap[norm(v.studentId)] = v.videoUrl ?? '';
  }

  const GRADES_DISPLAY = ALL_GRADES.slice().reverse() as GradeLetter[];
  const zip = new JSZip();

  for (const student of students) {
    const doc = new jsPDF({ orientation: 'landscape', unit: 'mm', format: 'letter' });

    doc.setFontSize(14);
    doc.text(`${exam.name} - ${groupName.value}`, 14, 14);
    doc.setFontSize(11);
    doc.text(`${student.lastName}, ${student.firstName}  (D.A.: ${student.number})`, 14, 22);

    let startY = 28;

    const videoUrl = videoUrlMap[norm(student.id)] ?? '';
    if (videoUrl) {
      doc.setFontSize(9);
      doc.setTextColor(25, 118, 210);
      doc.textWithLink(`Video : ${videoUrl}`, 14, startY, { url: videoUrl });
      doc.setTextColor(0, 0, 0);
      startY += 6;
    }

    for (const comp of competencies) {
      doc.setFontSize(10);
      doc.setFont('helvetica', 'bold');
      doc.text(comp.name, 14, startY);
      startY += 2;

      const headers = ['Critere', ...GRADES_DISPLAY, 'Note attribuee', 'Valeur'];
      const body: string[][] = [];

      const sid = norm(student.id);
      const compId = norm(comp.id);

      if (comp.criteria.length > 0) {
        for (const crit of comp.criteria) {
          const cid = norm(crit.id);
          const critGrade = critEvalMap[sid]?.[cid]?.grade ?? null;
          const valueStr =
            critGrade && crit.rawWeights[critGrade] != null
              ? `${crit.rawWeights[critGrade]}/${crit.totalValue}`
              : '';

          const row = [crit.label];
          for (const g of GRADES_DISPLAY) {
            if (critGrade === g) {
              const desc = crit.descriptions[g] ?? '';
              const pct = crit.weights[g] != null ? `${crit.weights[g]}%` : '';
              row.push(desc ? `${desc}\n${pct}` : `${g} (${pct})`);
            } else {
              const desc = crit.descriptions[g] ?? '';
              const pct = crit.weights[g] != null ? `${crit.weights[g]}%` : '';
              row.push(desc ? `${desc}\n${pct}` : '');
            }
          }
          row.push(critGrade ?? '—', valueStr);
          body.push(row);
        }

        const compGrade = compEvalMap[sid]?.[compId]?.grade ?? null;
        const totalMax = comp.criteria.reduce((s, c) => s + (c.totalValue ?? 0), 0);
        let obtained = 0;
        for (const crit of comp.criteria) {
          const g = critEvalMap[sid]?.[norm(crit.id)]?.grade;
          if (g && crit.rawWeights[g] != null) obtained += crit.rawWeights[g];
        }
        const valueStr = totalMax > 0 ? `${obtained}/${totalMax}` : '';
        const summaryRow = [`RESULTAT: ${comp.name}`];
        for (const g of GRADES_DISPLAY) {
          summaryRow.push(compGrade === g ? g : '');
        }
        summaryRow.push(compGrade ?? '—', valueStr);
        body.push(summaryRow);
      } else {
        const compGrade = compEvalMap[sid]?.[compId]?.grade ?? null;
        const row = ['Note directe'];
        for (const g of GRADES_DISPLAY) {
          row.push(compGrade === g ? g : '');
        }
        row.push(compGrade ?? '—', '');
        body.push(row);
      }

      const noteColIdx = GRADES_DISPLAY.length + 1;
      const valueColIdx = GRADES_DISPLAY.length + 2;

      autoTable(doc, {
        startY,
        head: [headers],
        body,
        theme: 'grid',
        styles: { fontSize: 7, cellPadding: 1.5 },
        headStyles: { fillColor: [45, 45, 68], textColor: 255, fontSize: 7 },
        columnStyles: {
          0: { cellWidth: 38 },
          ...Object.fromEntries(
            GRADES_DISPLAY.map((_, i) => [i + 1, { cellWidth: 30 }])
          ),
          [noteColIdx]: { cellWidth: 16, halign: 'center' as const, fontStyle: 'bold' as const },
          [valueColIdx]: { cellWidth: 16, halign: 'center' as const },
        },
        didParseCell: (data: any) => {
          if (
            data.section === 'body' &&
            data.column.index >= 1 &&
            data.column.index <= GRADES_DISPLAY.length
          ) {
            if (data.cell.raw && data.cell.raw !== '') {
              const rowIdx = data.row.index;
              const isSelected = rowIdx < comp.criteria.length
                ? critEvalMap[sid]?.[norm(comp.criteria[rowIdx].id)]?.grade === GRADES_DISPLAY[data.column.index - 1]
                : body[rowIdx][data.column.index] !== '';
              if (isSelected && (rowIdx >= comp.criteria.length || critEvalMap[sid]?.[norm(comp.criteria[rowIdx].id)]?.grade)) {
                data.cell.styles.fillColor = [76, 175, 80];
                data.cell.styles.textColor = 255;
                data.cell.styles.fontStyle = 'bold';
              }
            }
          }
          if (data.section === 'body' && data.column.index === noteColIdx) {
            const val = String(data.cell.raw).trim();
            if (val === 'A') { data.cell.styles.fillColor = [0, 172, 193]; data.cell.styles.textColor = 255; }
            else if (val === 'B') { data.cell.styles.fillColor = [67, 160, 71]; data.cell.styles.textColor = 255; }
            else if (val === 'C') { data.cell.styles.fillColor = [249, 168, 37]; data.cell.styles.textColor = 255; }
            else if (val === 'D') { data.cell.styles.fillColor = [251, 140, 0]; data.cell.styles.textColor = 255; }
            else if (val === 'E') { data.cell.styles.fillColor = [229, 57, 53]; data.cell.styles.textColor = 255; }
          }
          if (data.section === 'body' && data.row.index === body.length - 1 && comp.criteria.length > 0) {
            data.cell.styles.fillColor = data.cell.styles.fillColor ?? [220, 220, 220];
            data.cell.styles.fontStyle = 'bold';
            data.cell.styles.fontSize = 8;
          }
        },
      });

      startY = (doc as any).lastAutoTable.finalY + 6;
    }

    const sid = norm(student.id);
    const gradedComps = competencies.filter((c) => compEvalMap[sid]?.[norm(c.id)]?.grade);
    if (gradedComps.length > 0) {
      const sum = gradedComps.reduce((acc, c) => {
        const g = compEvalMap[sid][norm(c.id)].grade as GradeLetter;
        return acc + GRADE_VALUES[g];
      }, 0);
      const avg = sum / gradedComps.length;
      const letter = numericToLetter(avg);
      doc.setFontSize(11);
      doc.setFont('helvetica', 'bold');
      doc.text(`Moyenne: ${letter} (${avg.toFixed(1)}%)`, 14, startY);
    }

    const pdfBlob = doc.output('arraybuffer');
    zip.file(`${student.number}_${student.lastName}_${student.firstName}.pdf`, pdfBlob);
  }

  const zipBlob = await zip.generateAsync({ type: 'blob' });
  saveAs(zipBlob, `${exam.name}_${groupName.value}.zip`);
}

onMounted(async () => {
  await fetchGroupName();
  await fetchExams();
});

watch([classId, groupId], async () => {
  await fetchGroupName();
  await fetchExams();
});
</script>

<style scoped>
.groups-empty {
  padding: 32px;
  text-align: center;
  opacity: 0.6;
  font-weight: 600;
}
.groups-list {
  list-style: none;
  margin: 0;
  padding: 0;
}
.groups-list__item {
  display: flex;
  align-items: center;
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
}
.groups-list__item:last-child {
  border-bottom: none;
}
.groups-list__link {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 14px;
  padding: 16px 20px;
  text-decoration: none;
  color: inherit;
  transition: background-color 0.15s;
}
.groups-list__link:hover {
  background-color: rgba(0, 0, 0, 0.04);
}
.groups-list__icon {
  font-size: 22px;
}
.groups-list__info {
  flex: 1;
  display: flex;
  flex-direction: column;
  gap: 2px;
}
.groups-list__name {
  font-weight: 700;
  font-size: 15px;
}
.groups-list__arrow {
  font-size: 22px;
  opacity: 0.35;
}
.groups-list__export {
  margin-right: 8px;
  padding: 4px 10px;
  border-radius: 6px;
  border: 1px solid #10b981;
  background: transparent;
  color: #10b981;
  cursor: pointer;
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 0.5px;
  transition: background-color 0.15s, color 0.15s;
}
.groups-list__export:hover {
  background-color: #10b981;
  color: white;
}
.groups-list__delete {
  margin-right: 14px;
  width: 32px;
  height: 32px;
  border-radius: 8px;
  border: none;
  background: rgba(0, 0, 0, 0.08);
  cursor: pointer;
  font-size: 13px;
  font-weight: 900;
  opacity: 0.55;
  transition: opacity 0.15s, background-color 0.15s;
}
.groups-list__delete:hover {
  opacity: 1;
  background-color: rgba(220, 50, 50, 0.15);
}
</style>
