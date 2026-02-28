<template>
  <div class="evaluation">
    <!-- Loading state -->
    <div v-if="loading" class="evaluation__loading">
      <p>{{ t("evaluation.loading") }}</p>
    </div>

    <template v-else>
      <!-- Left panel: student list -->
      <aside class="evaluation__students">
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

      <!-- Right panel: grading table -->
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

        <p v-if="competencies.length === 0" class="evaluation__empty-msg">
          {{ t("evaluation.noSkills") }}
        </p>

        <div class="evaluation__table-wrapper" v-if="competencies.length > 0">
          <table class="evaluation__table">
            <thead>
              <tr>
                <th class="evaluation__th evaluation__th--competence">{{ t("evaluation.competence") }}</th>
                <th class="evaluation__th evaluation__th--grade">{{ t("evaluation.grade") }}</th>
                <th class="evaluation__th evaluation__th--comment">{{ t("evaluation.comments") }}</th>
              </tr>
            </thead>
            <tbody>
              <template v-for="comp in competencies" :key="comp.id">
                <!-- Competency header row -->
                <tr class="evaluation__competency-header">
                  <td class="evaluation__td evaluation__td--competence">
                    <span class="evaluation__color-dot" :style="{ backgroundColor: comp.color }"></span>
                    {{ comp.name }}
                  </td>
                  <td class="evaluation__td evaluation__td--grade">
                    <!-- If competency has criteria: show auto-computed grade -->
                    <template v-if="comp.criteria.length > 0">
                      <span
                        v-if="getGrade(comp.id)"
                        class="evaluation__computed-grade"
                        :class="`evaluation__computed-grade--${getGrade(comp.id)}`"
                      >
                        {{ getGrade(comp.id) }}
                      </span>
                      <span v-else class="evaluation__computed-grade evaluation__computed-grade--empty">—</span>
                    </template>
                    <!-- If no criteria: manual grade buttons (old behavior) -->
                    <div v-else class="evaluation__grade-tiles">
                      <button
                        v-for="grade in ALL_GRADES"
                        :key="grade"
                        type="button"
                        class="evaluation__grade-btn"
                        :class="{
                          'evaluation__grade-btn--selected': getGrade(comp.id) === grade,
                          [`evaluation__grade-btn--${grade}`]: true,
                        }"
                        @click="setGrade(comp.id, grade)"
                      >
                        {{ grade }}
                      </button>
                    </div>
                  </td>
                  <td class="evaluation__td evaluation__td--comment">
                    <input
                      type="text"
                      class="evaluation__comment-input"
                      :value="getComment(comp.id)"
                      :placeholder="t('evaluation.commentPlaceholder')"
                      @input="setComment(comp.id, ($event.target as HTMLInputElement).value)"
                    />
                  </td>
                </tr>
                <!-- Criterion rows: each with its own grade buttons -->
                <tr
                  v-for="crit in comp.criteria"
                  :key="crit.id"
                  class="evaluation__criterion-row"
                >
                  <td class="evaluation__td evaluation__criterion-name">
                    {{ crit.label }}
                  </td>
                  <td class="evaluation__td evaluation__criterion-grade">
                    <div class="evaluation__grade-tiles">
                      <button
                        v-for="grade in crit.options"
                        :key="grade"
                        type="button"
                        class="evaluation__grade-btn evaluation__grade-btn--sm"
                        :class="{
                          'evaluation__grade-btn--selected': getCriterionGrade(crit.id) === grade,
                          [`evaluation__grade-btn--${grade}`]: true,
                        }"
                        :title="criterionTooltip(crit, grade)"
                        @click="setCriterionGrade(crit.id, grade)"
                      >
                        {{ grade }}
                      </button>
                    </div>
                  </td>
                  <td class="evaluation__td evaluation__criterion-comment">
                    <input
                      type="text"
                      class="evaluation__criterion-comment-input"
                      :value="getCriterionComment(crit.id)"
                      :placeholder="t('evaluation.commentPlaceholder')"
                      @input="setCriterionComment(crit.id, ($event.target as HTMLInputElement).value)"
                    />
                  </td>
                </tr>
              </template>
            </tbody>
          </table>
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
import { ref, reactive, computed, onMounted } from "vue";
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

// ── Interfaces ───────────────────────────────────────────────

interface Student {
  id: string;
  name: string;
}

interface CriterionInfo {
  id: string;
  label: string;
  options: GradeLetter[];
  weights: Record<string, number>;
  descriptions: Record<string, string>;
  competencyId: string;
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
    const [studentsRes, skillsRes] = await Promise.all([
      fetch(`/api/classes/${props.classId}/students`),
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
          const descriptions: Record<string, string> = {};
          const totalValue = c.totalValue ?? 0;

          for (const w of c.weights ?? []) {
            if (w.isEnabled) {
              enabledLetters.add(w.weight);
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
            descriptions,
            competencyId: sk.skillId,
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

function ensureStudentEval(studentId: string) {
  if (!evaluations[studentId]) {
    evaluations[studentId] = {};
    for (const comp of competencies.value) {
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
  return evaluations[sid][compId].grade;
}

function setGrade(compId: string, grade: GradeLetter) {
  const sid = selectedStudentId.value;
  if (!sid) return;
  ensureStudentEval(sid);
  evaluations[sid][compId].grade = evaluations[sid][compId].grade === grade ? null : grade;
}

function getComment(compId: string): string {
  const sid = selectedStudentId.value;
  if (!sid) return "";
  ensureStudentEval(sid);
  return evaluations[sid][compId].comment;
}

function setComment(compId: string, value: string) {
  const sid = selectedStudentId.value;
  if (!sid) return;
  ensureStudentEval(sid);
  evaluations[sid][compId].comment = value;
}

// ── State: criterion evaluations per student ─────────────────

// studentId → criterionId → { grade, comment }
const criterionEvals = reactive<Record<string, Record<string, { grade: GradeLetter | null; comment: string }>>>({});

function ensureCriterionEval(studentId: string) {
  if (!criterionEvals[studentId]) {
    criterionEvals[studentId] = {};
    for (const comp of competencies.value) {
      for (const crit of comp.criteria) {
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
}

function autoGradeFromCriteria(studentId: string, critId: string) {
  const comp = competencies.value.find((c) =>
    c.criteria.some((cr) => cr.id === critId)
  );
  if (!comp || comp.criteria.length === 0) return;

  // Collect numeric values of all graded criteria
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
}

// ── Tooltip helper ───────────────────────────────────────────

function criterionTooltip(crit: CriterionInfo, grade: GradeLetter): string {
  const desc = crit.descriptions[grade];
  const pct = crit.weights[grade];
  if (desc && pct != null) return `${grade} (${pct}%) : ${desc}`;
  if (desc) return `${grade} : ${desc}`;
  if (pct != null) return `${grade} : ${pct}%`;
  return grade;
}

// ── Preview modal ────────────────────────────────────────────

const showPreview = ref(false);

// ── Average calculation ──────────────────────────────────────

const averageNumeric = computed<number | null>(() => {
  const sid = selectedStudentId.value;
  if (!sid) return null;
  ensureStudentEval(sid);
  const evals = evaluations[sid];
  const graded = competencies.value.filter((c) => evals[c.id].grade !== null);
  if (graded.length === 0) return null;
  const sum = graded.reduce((acc, c) => acc + GRADE_VALUES[evals[c.id].grade!], 0);
  return sum / graded.length;
});

const averageLetter = computed<GradeLetter | null>(() => {
  if (averageNumeric.value === null) return null;
  return numericToLetter(averageNumeric.value);
});
</script>
