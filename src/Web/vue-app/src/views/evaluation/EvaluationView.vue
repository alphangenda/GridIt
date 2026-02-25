<template>
  <div class="evaluation">
    <!-- Left panel: student list -->
    <aside class="evaluation__students">
      <h2 class="evaluation__students-title">{{ t("evaluation.students") }}</h2>
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

      <div class="evaluation__table-wrapper">
        <table class="evaluation__table">
          <thead>
            <tr>
              <th class="evaluation__th evaluation__th--competence">{{ t("evaluation.competence") }}</th>
              <th class="evaluation__th evaluation__th--grade">{{ t("evaluation.grade") }}</th>
              <th class="evaluation__th evaluation__th--comment">{{ t("evaluation.difficulties") }}</th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="(comp, idx) in competencies" :key="comp.id">
              <td class="evaluation__td evaluation__td--competence">
                <span class="evaluation__color-dot" :style="{ backgroundColor: comp.color }"></span>
                {{ comp.name }}
              </td>
              <td class="evaluation__td evaluation__td--grade">
                <div class="evaluation__grade-tiles">
                  <button
                    v-for="grade in comp.options"
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
          </tbody>
        </table>
      </div>
    </section>

    <!-- Empty state -->
    <section class="evaluation__empty" v-else>
      <p>{{ t("evaluation.selectStudent") }}</p>
    </section>
  </div>
</template>

<script setup lang="ts">
import { ref, reactive, computed } from "vue";
import { useI18n } from "vue3-i18n";
import {
  type GradeLetter,
  GRADE_VALUES,
  ALL_GRADES,
  THREE_GRADES,
  numericToLetter,
} from "@/config/gradeConfig";

const { t } = useI18n();

// ── Static data ──────────────────────────────────────────────

interface Student {
  id: string;
  name: string;
}

interface Competency {
  id: string;
  name: string;
  color: string;
  options: GradeLetter[];
}

const students: Student[] = [
  { id: "s1", name: "Ava Tremblay" },
  { id: "s2", name: "Adam Bélanger" },
  { id: "s3", name: "Ismaël Dupont" },
  { id: "s4", name: "Léo Gagnon" },
  { id: "s5", name: "Clara Morin" },
  { id: "s6", name: "Noah Lavoie" },
];

const competencies: Competency[] = [
  { id: "c1", name: "Configuration et qualité générale", color: "#6366f1", options: THREE_GRADES },
  { id: "c2", name: "Administration Django", color: "#f59e0b", options: ALL_GRADES },
  { id: "c3", name: "Templates et interface utilisateur", color: "#10b981", options: ALL_GRADES },
  { id: "c4", name: "API externe et fonctionnalités avancées", color: "#ef4444", options: ALL_GRADES },
  { id: "c5", name: "Formulaires et validations", color: "#8b5cf6", options: THREE_GRADES },
  { id: "c6", name: "Vues principales et logique", color: "#3b82f6", options: ALL_GRADES },
  { id: "c7", name: "Authentification et autorisations", color: "#ec4899", options: ALL_GRADES },
  { id: "c8", name: "Modèles et base de données", color: "#14b8a6", options: THREE_GRADES },
  { id: "c9", name: "Analyser le projet de développement", color: "#f97316", options: ALL_GRADES },
];

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
    for (const comp of competencies) {
      evaluations[studentId][comp.id] = { grade: null, comment: "" };
    }
  }
}

// ── Selection ────────────────────────────────────────────────

const selectedStudentId = ref<string | null>(null);
const selectedStudent = computed(() =>
  students.find((s) => s.id === selectedStudentId.value) ?? null
);

// ── Grade / comment accessors ────────────────────────────────

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

// ── Average calculation ──────────────────────────────────────

const averageNumeric = computed<number | null>(() => {
  const sid = selectedStudentId.value;
  if (!sid) return null;
  ensureStudentEval(sid);
  const evals = evaluations[sid];
  const graded = competencies.filter((c) => evals[c.id].grade !== null);
  if (graded.length === 0) return null;
  const sum = graded.reduce((acc, c) => acc + GRADE_VALUES[evals[c.id].grade!], 0);
  return sum / graded.length;
});

const averageLetter = computed<GradeLetter | null>(() => {
  if (averageNumeric.value === null) return null;
  return numericToLetter(averageNumeric.value);
});
</script>
