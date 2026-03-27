<template>
  <div class="evaluation">
    <!-- Loading state -->
    <div v-if="loading" class="evaluation__loading">
      <p>{{ t("evaluation.loading") }}</p>
    </div>

    <template v-else>
      <!-- Left panel: student list -->
      <aside class="evaluation__students">
        <router-link
          :to="{ name: 'classes.examDetail', params: { classId: props.classId, examId: props.examId } }"
          class="back-link evaluation__back-link"
        >
          &lt; Retour
        </router-link>
        <h2 class="evaluation__students-title">{{ t("evaluation.students") }}</h2>
        <p v-if="students.length === 0" class="evaluation__empty-msg">
          {{ t("evaluation.noStudents") }}
        </p>
        <ul class="evaluation__students-list">
          <li v-for="student in students" :key="student.id">
            <button
              type="button"
              class="evaluation__student-btn"
              :class="{ 'evaluation__student-btn--active': selectedStudentId === student.id }"
              @click="selectedStudentId = student.id"
            >
              {{ student.name }}
            </button>
          </li>
        </ul>
      </aside>

      <!-- Right panel: grading grid -->
      <section class="evaluation__grading" v-if="selectedStudent">
        <div class="evaluation__grading-header">
          <h2 class="evaluation__grading-title">{{ selectedStudent.name }}</h2>
          <button
            type="button"
            class="evaluation__preview-btn"
            @click="showPreview = true"
          >
            {{ t("evaluation.preview") }}
          </button>
          <div class="evaluation__average">
            <span class="evaluation__average-label">{{ t("evaluation.average") }}</span>
            <span
              class="evaluation__average-letter"
              :class="`evaluation__average-letter--${averageLetter}`"
            >
              {{ averageLetter ?? "—" }}
            </span>
            <span class="evaluation__average-numeric" v-if="averageNumeric !== null">
              ({{ averageNumeric.toFixed(1) }}%)
            </span>
          </div>
        </div>

        <div class="evaluation__video-url">
          <label class="evaluation__video-url-label" for="videoUrlInput">
            Lien YouTube
          </label>
          <input
            id="videoUrlInput"
            type="url"
            class="evaluation__video-url-input"
            placeholder="https://www.youtube.com/watch?v=..."
            :value="getVideoUrl()"
            @input="setVideoUrl(($event.target as HTMLInputElement).value)"
          />
        </div>

        <p v-if="competencies.length === 0" class="evaluation__empty-msg">
          {{ t("evaluation.noSkills") }}
        </p>

        <div class="evaluation__table-wrapper" v-if="competencies.length > 0">
          <div v-for="comp in competencies" :key="comp.id" class="evaluation__comp-block">
            <table class="evaluation__grid-table">
              <thead>
                <tr>
                  <th class="grid-th grid-th--element">
                    <span class="evaluation__color-dot" :style="{ backgroundColor: comp.color }"></span>
                    {{ comp.name }}
                  </th>
                  <th
                    v-for="grade in GRADES_DISPLAY"
                    :key="grade"
                    class="grid-th grade-header"
                    :class="grade"
                  >
                    {{ grade }}
                  </th>
                  <th class="grid-th grid-th--note">Note</th>
                  <th class="grid-th grid-th--valeur">Valeur</th>
                  <th class="grid-th grid-th--comment">{{ t("evaluation.comments") }}</th>
                </tr>
              </thead>
              <tbody>
                <!-- Criterion rows -->
                <tr v-for="crit in comp.criteria" :key="crit.id" class="grid-criterion-row">
                  <td class="grid-td grid-td--element">{{ crit.label }}</td>
                  <td
                    v-for="grade in GRADES_DISPLAY"
                    :key="grade"
                    class="grade-cell"
                    :class="{
                      'grade-selected': getCriterionGrade(crit.id) === grade,
                      'grade-cell--disabled': !crit.options.includes(grade),
                    }"
                    @click="crit.options.includes(grade) ? setCriterionGrade(crit.id, grade) : undefined"
                  >
                    <span v-if="crit.descriptions[grade]" class="grade-cell-desc">{{ crit.descriptions[grade] }}</span>
                    <span v-if="crit.weights[grade] != null" class="grade-cell-pct">{{ crit.weights[grade] }}%</span>
                  </td>
                  <td class="grid-td grid-td--note">{{ criterionNoteDisplay(crit) }}</td>
                  <td class="grid-td grid-td--valeur">{{ crit.totalValue > 0 ? crit.totalValue : '' }}</td>
                  <td class="grid-td grid-td--comment">
                    <input
                      type="text"
                      class="evaluation__criterion-comment-input"
                      :value="getCriterionComment(crit.id)"
                      :placeholder="t('evaluation.commentPlaceholder')"
                      @input="setCriterionComment(crit.id, ($event.target as HTMLInputElement).value)"
                    />
                  </td>
                </tr>
                <!-- Note compétence row (when criteria exist) -->
                <tr v-if="comp.criteria.length > 0" class="grid-competency-note-row">
                  <td class="grid-td grid-td--element">{{ t("evaluation.competencyGrade") }}</td>
                  <td
                    v-for="grade in GRADES_DISPLAY"
                    :key="grade"
                    class="grade-cell grade-cell--summary"
                    :class="{ 'grade-selected': getGrade(comp.id) === grade }"
                  >
                    <span v-if="getGrade(comp.id) === grade">{{ grade }}</span>
                  </td>
                  <td class="grid-td grid-td--note">{{ compNoteDisplay(comp) }}</td>
                  <td class="grid-td grid-td--valeur">{{ compTotalValue(comp) > 0 ? compTotalValue(comp) : '' }}</td>
                  <td class="grid-td grid-td--comment">
                    <input
                      type="text"
                      class="evaluation__comment-input"
                      :value="getComment(comp.id)"
                      :placeholder="t('evaluation.commentPlaceholder')"
                      @input="setComment(comp.id, ($event.target as HTMLInputElement).value)"
                    />
                  </td>
                </tr>
                <!-- Manual grade row (when no criteria) -->
                <tr v-if="comp.criteria.length === 0" class="grid-manual-row">
                  <td class="grid-td grid-td--element">{{ t("evaluation.grade") }}</td>
                  <td
                    v-for="grade in GRADES_DISPLAY"
                    :key="grade"
                    class="grade-cell"
                    :class="{ 'grade-selected': getGrade(comp.id) === grade }"
                    @click="setGrade(comp.id, grade)"
                  >
                    <span class="grade-cell-letter">{{ grade }}</span>
                  </td>
                  <td class="grid-td grid-td--note"></td>
                  <td class="grid-td grid-td--valeur"></td>
                  <td class="grid-td grid-td--comment">
                    <input
                      type="text"
                      class="evaluation__comment-input"
                      :value="getComment(comp.id)"
                      :placeholder="t('evaluation.commentPlaceholder')"
                      @input="setComment(comp.id, ($event.target as HTMLInputElement).value)"
                    />
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </section>

      <!-- Empty state -->
      <section class="evaluation__empty" v-else>
        <p>{{ t("evaluation.selectStudent") }}</p>
      </section>

      <!-- Preview modal -->
      <FullScreenModal v-model="showPreview" class="modal--expanded">
        <div class="evaluation__preview">
          <div class="evaluation__preview-header">
            <h2 class="evaluation__preview-title">
              {{ selectedStudent?.name }}
            </h2>
            <div class="evaluation__preview-average" v-if="averageLetter">
              <span class="evaluation__average-label">{{ t("evaluation.average") }}</span>
              <span
                class="evaluation__average-letter"
                :class="`evaluation__average-letter--${averageLetter}`"
              >
                {{ averageLetter }}
              </span>
              <span class="evaluation__average-numeric" v-if="averageNumeric !== null">
                ({{ averageNumeric.toFixed(1) }}%)
              </span>
            </div>
          </div>

          <div
            v-for="comp in competencies"
            :key="comp.id"
            class="evaluation__preview-comp"
          >
            <table class="evaluation__preview-table">
              <thead>
                <tr>
                  <th class="evaluation__preview-th evaluation__preview-th--label">
                    <span class="evaluation__color-dot" :style="{ backgroundColor: comp.color }"></span>
                    {{ comp.name }}
                  </th>
                  <th
                    v-for="grade in ALL_GRADES.slice().reverse()"
                    :key="grade"
                    class="evaluation__preview-th"
                    :class="`evaluation__preview-th--${grade}`"
                  >
                    {{ grade }}
                  </th>
                  <th class="evaluation__preview-th evaluation__preview-th--comment">
                    {{ t("evaluation.comments") }}
                  </th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="crit in comp.criteria" :key="crit.id">
                  <td class="evaluation__preview-td evaluation__preview-td--label">
                    {{ crit.label }}
                  </td>
                  <td
                    v-for="grade in ALL_GRADES.slice().reverse()"
                    :key="grade"
                    class="evaluation__preview-td"
                    :class="{
                      'evaluation__preview-cell--selected': getCriterionGrade(crit.id) === grade,
                      [`evaluation__preview-cell--${grade}`]: getCriterionGrade(crit.id) === grade,
                    }"
                  >
                    <div class="evaluation__preview-desc">{{ crit.descriptions[grade] ?? '' }}</div>
                    <div class="evaluation__preview-pts" v-if="crit.weights[grade] != null">
                      {{ crit.weights[grade] }}%
                    </div>
                  </td>
                  <td class="evaluation__preview-td evaluation__preview-td--comment">
                    {{ getCriterionComment(crit.id) }}
                  </td>
                </tr>
                <!-- Summary row -->
                <tr class="evaluation__preview-summary">
                  <td class="evaluation__preview-td evaluation__preview-td--label">
                    {{ t("evaluation.competencyGrade") }}
                  </td>
                  <td
                    v-for="grade in ALL_GRADES.slice().reverse()"
                    :key="grade"
                    class="evaluation__preview-td evaluation__preview-td--summary"
                    :class="{
                      'evaluation__preview-cell--selected': getGrade(comp.id) === grade,
                      [`evaluation__preview-cell--${grade}`]: getGrade(comp.id) === grade,
                    }"
                  >
                    <span v-if="getGrade(comp.id) === grade">{{ grade }}</span>
                  </td>
                  <td class="evaluation__preview-td evaluation__preview-td--comment">
                    {{ getComment(comp.id) }}
                  </td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>
      </FullScreenModal>
    </template>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed, onMounted, watch } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
import {
  type GradeLetter,
  GRADE_VALUES,
  ALL_GRADES,
  numericToLetter,
} from "@/config/gradeConfig";
import FullScreenModal from "@/components/popups/FullScreenModal.vue";

const props = defineProps<{
  classId: string;
  examId: string;
}>();

const { t } = useI18n();
const route = useRoute();

// E → A (left to right), matching cégep grille convention
const GRADES_DISPLAY = ALL_GRADES.slice().reverse() as GradeLetter[];

// ── Interfaces ───────────────────────────────────────────────

interface Student {
  id: string;
  name: string;
}

interface CriterionInfo {
  id: string;
  label: string;
  options: GradeLetter[];
  weights: Record<string, number>;      // percentage per grade
  rawWeights: Record<string, number>;   // raw value per grade
  descriptions: Record<string, string>;
  competencyId: string;
  totalValue: number;
}

interface Competency {
  id: string;
  name: string;
  color: string;
  criteria: CriterionInfo[];
}

// ── Palette de couleurs pour les compétences ─────────────────

const COLORS = [
  "#6366f1", "#f59e0b", "#10b981", "#ef4444", "#8b5cf6",
  "#3b82f6", "#ec4899", "#14b8a6", "#f97316",
];

// ── State ────────────────────────────────────────────────────

const loading = ref(true);
const students = ref<Student[]>([]);
const competencies = ref<Competency[]>([]);

// ── Fetch data from API ──────────────────────────────────────

onMounted(async () => {
  try {
    const groupId = route.query.groupId as string | undefined;

    const studentsUrl = groupId
      ? `/api/exams/${props.examId}/groups/${groupId}/students`
      : `/api/classes/${props.classId}/students`;

    const [studentsRes, skillsRes] = await Promise.all([
      fetch(studentsUrl),
      fetch(`/api/exams/${props.examId}/skills`),
    ]);

    const studentsData = await studentsRes.json();
    const skillsData = await skillsRes.json();

    students.value = studentsData.map((s: any) => ({
      id: s.id,
      name: `${s.firstName} ${s.lastName}`,
    }));

    const skillsWithCriteria = await Promise.all(
      skillsData.map(async (sk: any, idx: number) => {
        const criteriaRes = await fetch(
          `/api/exams/${props.examId}/skills/${sk.skillId}/criteria`
        );
        const criteriaData = await criteriaRes.json();

        const criteria: CriterionInfo[] = criteriaData.map((c: any) => {
          const enabledLetters = new Set<string>();
          const weightPercents: Record<string, number> = {};
          const rawValues: Record<string, number> = {};
          const descriptions: Record<string, string> = {};
          const totalValue = c.totalValue ?? 0;

          for (const w of c.weights ?? []) {
            if (w.isEnabled) {
              enabledLetters.add(w.weight);
              rawValues[w.weight] = w.value ?? 0;
              weightPercents[w.weight] = totalValue > 0
                ? Math.round((w.value / totalValue) * 100)
                : 0;
              if (w.description) {
                descriptions[w.weight] = w.description;
              }
            }
          }

          const options: GradeLetter[] = enabledLetters.size > 0
            ? ALL_GRADES.filter((g) => enabledLetters.has(g))
            : ALL_GRADES;

          return {
            id: c.id,
            label: c.label,
            options,
            weights: weightPercents,
            rawWeights: rawValues,
            descriptions,
            competencyId: sk.skillId,
            totalValue,
          };
        });

        return {
          id: sk.skillId,
          name: sk.label,
          color: COLORS[idx % COLORS.length],
          criteria,
        };
      })
    );

    competencies.value = skillsWithCriteria;

    // Load persisted evaluations from DB
    try {
      const evalRes = await fetch(`/api/exams/${props.examId}/evaluations`);
      if (evalRes.ok) {
        const evalData = await evalRes.json();
        for (const e of evalData.competencyEvaluations ?? []) {
          if (!evaluations[e.studentId]) evaluations[e.studentId] = {};
          evaluations[e.studentId][e.competencyId] = {
            grade: (e.grade as GradeLetter) || null,
            comment: e.comment ?? "",
          };
        }
        for (const e of evalData.criterionEvaluations ?? []) {
          if (!criterionEvals[e.studentId]) criterionEvals[e.studentId] = {};
          criterionEvals[e.studentId][e.criterionId] = {
            grade: (e.grade as GradeLetter) || null,
            comment: e.comment ?? "",
          };
        }
        for (const v of evalData.videoUrls ?? []) {
          videoUrls[v.studentId] = v.videoUrl ?? "";
        }
      }
    } catch { /* silently ignore load errors */ }
  } finally {
    loading.value = false;
  }
});

// ── State: evaluations per student ───────────────────────────

interface CompEval {
  grade: GradeLetter | null;
  comment: string;
}

// studentId → competencyId → evaluation
const evaluations = reactive<Record<string, Record<string, CompEval>>>({});

// studentId → YouTube video URL
const videoUrls = reactive<Record<string, string>>({});

function ensureStudentEval(studentId: string) {
  if (!evaluations[studentId]) {
    evaluations[studentId] = {};
  }
  for (const comp of competencies.value) {
    if (!evaluations[studentId][comp.id]) {
      evaluations[studentId][comp.id] = { grade: null, comment: "" };
    }
  }
}

// ── Selection ────────────────────────────────────────────────

const selectedStudentId = ref<string | null>(null);
const selectedStudent = computed(() =>
  students.value.find((s) => s.id === selectedStudentId.value) ?? null
);

// ── Grade / comment accessors (competency level) ─────────────

function getGrade(compId: string): GradeLetter | null {
  const sid = selectedStudentId.value;
  if (!sid) return null;
  ensureStudentEval(sid);
  return evaluations[sid][compId]?.grade ?? null;
}

function setGrade(compId: string, grade: GradeLetter) {
  const sid = selectedStudentId.value;
  if (!sid) return;
  ensureStudentEval(sid);
  evaluations[sid][compId].grade = evaluations[sid][compId].grade === grade ? null : grade;
  triggerSave();
}

function getComment(compId: string): string {
  const sid = selectedStudentId.value;
  if (!sid) return "";
  ensureStudentEval(sid);
  return evaluations[sid][compId]?.comment ?? "";
}

function setComment(compId: string, value: string) {
  const sid = selectedStudentId.value;
  if (!sid) return;
  ensureStudentEval(sid);
  evaluations[sid][compId].comment = value;
  triggerSave();
}

// ── Video URL accessors ──────────────────────────────────────

function getVideoUrl(): string {
  const sid = selectedStudentId.value;
  if (!sid) return "";
  return videoUrls[sid] ?? "";
}

function setVideoUrl(value: string) {
  const sid = selectedStudentId.value;
  if (!sid) return;
  videoUrls[sid] = value;
  triggerSave();
}

// ── State: criterion evaluations per student ─────────────────

// studentId → criterionId → { grade, comment }
const criterionEvals = reactive<Record<string, Record<string, { grade: GradeLetter | null; comment: string }>>>({});

function ensureCriterionEval(studentId: string) {
  if (!criterionEvals[studentId]) {
    criterionEvals[studentId] = {};
  }
  for (const comp of competencies.value) {
    for (const crit of comp.criteria) {
      if (!criterionEvals[studentId][crit.id]) {
        criterionEvals[studentId][crit.id] = { grade: null, comment: "" };
      }
    }
  }
}

function getCriterionGrade(critId: string): GradeLetter | null {
  const sid = selectedStudentId.value;
  if (!sid) return null;
  ensureCriterionEval(sid);
  return criterionEvals[sid][critId]?.grade ?? null;
}

function setCriterionGrade(critId: string, grade: GradeLetter) {
  const sid = selectedStudentId.value;
  if (!sid) return;
  ensureCriterionEval(sid);
  ensureStudentEval(sid);
  if (!criterionEvals[sid][critId]) {
    criterionEvals[sid][critId] = { grade: null, comment: "" };
  }
  // Toggle: click same grade to deselect
  criterionEvals[sid][critId].grade = criterionEvals[sid][critId].grade === grade ? null : grade;
  // Auto-compute competency grade from criteria
  autoGradeFromCriteria(sid, critId);
  triggerSave();
}

function autoGradeFromCriteria(studentId: string, critId: string) {
  const comp = competencies.value.find((c) =>
    c.criteria.some((cr) => cr.id === critId)
  );
  if (!comp || comp.criteria.length === 0) return;

  const grades: number[] = [];
  for (const crit of comp.criteria) {
    const g = criterionEvals[studentId]?.[crit.id]?.grade;
    if (g != null) {
      grades.push(GRADE_VALUES[g]);
    }
  }

  if (grades.length === 0) {
    evaluations[studentId][comp.id].grade = null;
    return;
  }

  const avg = grades.reduce((a, b) => a + b, 0) / grades.length;
  evaluations[studentId][comp.id].grade = numericToLetter(avg);
}

function getCriterionComment(critId: string): string {
  const sid = selectedStudentId.value;
  if (!sid) return "";
  ensureCriterionEval(sid);
  return criterionEvals[sid][critId]?.comment ?? "";
}

function setCriterionComment(critId: string, value: string) {
  const sid = selectedStudentId.value;
  if (!sid) return;
  ensureCriterionEval(sid);
  if (!criterionEvals[sid][critId]) {
    criterionEvals[sid][critId] = { grade: null, comment: "" };
  }
  criterionEvals[sid][critId].comment = value;
  triggerSave();
}

// ── Note / Valeur display helpers ────────────────────────────

function criterionNoteDisplay(crit: CriterionInfo): string {
  if (!crit.totalValue) return "";
  const grade = getCriterionGrade(crit.id);
  if (grade != null && crit.rawWeights[grade] != null) {
    return `${crit.rawWeights[grade]} / ${crit.totalValue}`;
  }
  return `— / ${crit.totalValue}`;
}

function compTotalValue(comp: Competency): number {
  return comp.criteria.reduce((sum, c) => sum + (c.totalValue ?? 0), 0);
}

function compNoteDisplay(comp: Competency): string {
  const max = compTotalValue(comp);
  if (!max) return "";
  const sid = selectedStudentId.value;
  if (!sid) return `— / ${max}`;
  ensureCriterionEval(sid);
  let obtained = 0;
  for (const crit of comp.criteria) {
    const grade = criterionEvals[sid]?.[crit.id]?.grade;
    if (grade != null && crit.rawWeights[grade] != null) {
      obtained += crit.rawWeights[grade];
    }
  }
  return `${obtained} / ${max}`;
}

// ── Preview modal ────────────────────────────────────────────

const showPreview = ref(false);

// ── Average calculation ──────────────────────────────────────

const averageNumeric = computed<number | null>(() => {
  const sid = selectedStudentId.value;
  if (!sid) return null;
  ensureStudentEval(sid);
  const evals = evaluations[sid];
  const graded = competencies.value.filter((c) => evals[c.id]?.grade != null);
  if (graded.length === 0) return null;
  const sum = graded.reduce((acc, c) => {
    const grade = evals[c.id]?.grade;
    return acc + (grade ? GRADE_VALUES[grade] : 0);
  }, 0);
  return sum / graded.length;
});

const averageLetter = computed<GradeLetter | null>(() => {
  if (averageNumeric.value === null) return null;
  return numericToLetter(averageNumeric.value);
});

// ── Auto-save (debounced) ─────────────────────────────────────

let saveTimer: ReturnType<typeof setTimeout> | null = null;
const saveVersion = ref(0);

function triggerSave() {
  saveVersion.value++;
}

watch(saveVersion, () => {
  if (saveTimer) clearTimeout(saveTimer);
  saveTimer = setTimeout(async () => {
    const compEvals: { studentId: string; competencyId: string; grade: string | null; comment: string }[] = [];
    const critEvals: { studentId: string; criterionId: string; grade: string | null; comment: string }[] = [];

    for (const [sid, comps] of Object.entries(evaluations)) {
      for (const [compId, ev] of Object.entries(comps)) {
        compEvals.push({ studentId: sid, competencyId: compId, grade: ev.grade, comment: ev.comment });
      }
    }
    for (const [sid, crits] of Object.entries(criterionEvals)) {
      for (const [critId, ev] of Object.entries(crits)) {
        critEvals.push({ studentId: sid, criterionId: critId, grade: ev.grade, comment: ev.comment });
      }
    }

    const videoUrlPayloads: { studentId: string; videoUrl: string }[] = [];
    for (const [sid, url] of Object.entries(videoUrls)) {
      videoUrlPayloads.push({ studentId: sid, videoUrl: url });
    }

    try {
      await fetch(`/api/exams/${props.examId}/evaluations`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({
          competencyEvaluations: compEvals,
          criterionEvaluations: critEvals,
          videoUrls: videoUrlPayloads,
        }),
      });
    } catch { /* silently ignore save errors */ }
  }, 1500);
});
</script>

<style scoped>
/* ── Grid table layout ──────────────────────────────────────── */

.evaluation__comp-block {
  margin-bottom: 24px;
  overflow-x: auto;
}

.evaluation__grid-table {
  width: 100%;
  border-collapse: collapse;
  table-layout: auto;
}

/* Header cells */
.grid-th {
  padding: 8px 10px;
  font-weight: 600;
  text-align: center;
  border: 1px solid #e0e0e0;
  background-color: #f5f5f5;
  white-space: nowrap;
}

.grid-th--element {
  text-align: left;
  min-width: 160px;
  max-width: 220px;
}

.grid-th--note,
.grid-th--valeur {
  min-width: 80px;
}

.grid-th--comment {
  min-width: 140px;
}

/* Grade column header colors */
.grade-header.E { color: #e53935; }
.grade-header.D { color: #fb8c00; }
.grade-header.C { color: #f9a825; }
.grade-header.B { color: #43a047; }
.grade-header.A { color: #00acc1; }

/* Body cells */
.grid-td {
  padding: 8px 10px;
  border: 1px solid #e0e0e0;
  vertical-align: middle;
}

.grid-td--element {
  font-weight: 500;
  min-width: 160px;
  max-width: 220px;
}

.grid-td--note,
.grid-td--valeur {
  text-align: center;
  white-space: nowrap;
  font-size: 0.875rem;
  color: #555;
}

.grid-td--comment {
  min-width: 140px;
}

/* Grade cells (clickable) */
.grade-cell {
  cursor: pointer;
  padding: 8px 10px;
  border: 1px solid #e0e0e0;
  transition: background-color 0.15s;
  vertical-align: top;
  min-width: 150px;
  max-width: 200px;
}

.grade-cell:hover:not(.grade-cell--disabled):not(.grade-cell--summary) {
  background-color: #e8f5e9;
}

.grade-cell.grade-selected {
  background-color: #4caf50;
  color: white;
  font-weight: 500;
}

.grade-cell--disabled {
  cursor: default;
  background-color: #fafafa;
  color: #bbb;
}

.grade-cell--summary {
  cursor: default;
  text-align: center;
  font-weight: 600;
  font-size: 1rem;
}

.grade-cell-desc {
  display: block;
  font-size: 0.8rem;
  line-height: 1.4;
}

.grade-cell-pct {
  display: block;
  font-size: 0.75rem;
  margin-top: 4px;
  opacity: 0.75;
}

.grade-cell-letter {
  display: block;
  text-align: center;
  font-weight: 600;
  font-size: 1rem;
}

/* Competency note row */
.grid-competency-note-row td {
  background-color: #f5f5f5;
  font-weight: 600;
}

/* Comment input */
.evaluation__criterion-comment-input,
.evaluation__comment-input {
  width: 100%;
  padding: 4px 6px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.85rem;
  box-sizing: border-box;
}

/* ── YouTube video URL field ───────────────────────────────── */

.evaluation__video-url {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 18px;
  padding: 10px 14px;
  background-color: #f5f5f5;
  border: 1px solid #e0e0e0;
  border-radius: 6px;
}

.evaluation__video-url-label {
  font-weight: 600;
  font-size: 0.875rem;
  white-space: nowrap;
  color: #444;
}

.evaluation__video-url-input {
  flex: 1;
  padding: 6px 8px;
  border: 1px solid #ddd;
  border-radius: 4px;
  font-size: 0.85rem;
  box-sizing: border-box;
}
</style>
