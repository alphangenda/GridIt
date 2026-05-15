<template>
  <div class="exam-detail">
    <ConfirmResetPopup
      v-if="showResetConfirm"
      :is-loading="isResetting"
      @close="showResetConfirm = false"
      @confirm="confirmResetDefaults"
    />

    <div class="exam-detail__header">
      <div>
        <div class="exam-detail__back">
          <router-link
            :to="isReadOnly ? { name: 'grids' } : route.params.groupId ? { name: 'classes.groupExams', params: { classId: classId, groupId: String(route.params.groupId) } } : { name: 'classes.detail', params: { classId: classId } }"
            class="exam-detail__back-link"
            :aria-label="t('pages.examDetail.back')"
          >
            &lt;
          </router-link>
          <h1 class="exam-detail__title">Examen(s)</h1>
        </div>
        <p v-if="!isReadOnly" class="exam-detail__hint">{{ t("pages.examDetail.subtitle") }}</p>
      </div>

      <div class="exam-detail__actions" >
        <button v-if="!isReadOnly" type="button" class="btn btn--secondary" @click="showInfo = true" style="margin-right: 8px;">
          {{ t("pages.examDetail.skillGrid") }}
        </button>
        <router-link
          v-if="!isReadOnly"
          :to="{ name: 'evaluation', params: { classId: classId, examId: examId }, query: route.params.groupId ? { groupId: route.params.groupId } : {} }"
          class="btn"
        >
          {{ t("evaluation.evaluate") }}
        </router-link>
      </div>
    </div>

    <FullScreenModal v-model="showInfo" :class="{ 'modal--expanded': showSidePanel }">
      <div
        class="info-modal"
        :class="{ 'info-modal--expanded': showSidePanel }"
      >

        <div class="info-modal__top-actions">
          <button
            v-if="!isReadOnly"
            type="button"
            class="btn btn--reset"
            :disabled="isResetting"
            @click="showResetConfirm = true"
          >
            {{ isResetting ? t("pages.examDetail.resetting") : t("pages.examDetail.reset") }}
          </button>
          <button
            type="button"
            class="btn btn--secondary"
            @click="showSidePanel = !showSidePanel"
          >
            {{ showSidePanel ? t("pages.examDetail.closeStats") : t("pages.examDetail.openStats") }}
          </button>
        </div>

        <div class="info-modal__layout">

          <!-- =========================
          COLONNE GAUCHE
          ========================== -->
          <div class="info-modal__main">

            <h2 class="info-modal__section-title">{{ t("pages.examDetail.skills") }}</h2>

            <div class="skills-grid">
              <div class="skills-picker" v-if="!isReadOnly">
                <button
                  type="button"
                  class="skill-card skill-card--add"
                  @click="showPicker = !showPicker"
                >
                  <span class="skill-card__plus">+</span>
                  <span class="skill-card__label">{{ t("pages.examDetail.chooseSkills") }}</span>
                </button>

                <div
                  v-if="showPicker"
                  class="picker"
                  @mouseleave="showPicker = false"
                >
                  <div class="picker__title">{{ t("pages.examDetail.pickerTitle") }}</div>
                  <div class="picker__list">
                    <label
                      v-for="s in allSkills"
                      :key="s.id"
                      class="picker__item"
                    >
                      <input
                        type="checkbox"
                        :checked="isSelected(s.id)"
                        @change="toggleSkill(s)"
                      />
                      <span>{{ s.label }}</span>
                    </label>
                  </div>
                </div>
              </div>
            </div>

            <div class="skills-list">
              <button
                v-for="s in selectedSkills"
                :key="s.id"
                type="button"
                class="skill-card"
                :class="{ 'skill-card--active': activeSkillId === s.id }"
                @click="activeSkillId = s.id"
              >
                <span class="skill-card__dot" />
                <span class="skill-card__label">{{ s.label }}</span>
              </button>
            </div>

            <div class="description-section">
              <h2 class="info-modal__section-title">{{ t("pages.examDetail.description") }}</h2>

              <Card class="description-card">
                <div class="description-card__content">

                  <div class="description-left">
                    <h3 class="description-title">
                      {{ activeSkill?.label ?? t("pages.examDetail.noSkillSelected") }}
                    </h3>

                    <div class="criteria-header">
                      <div class="criteria-title">{{ t("pages.examDetail.criteria") }}</div>
                      <button
                        v-if="!isReadOnly"
                        type="button"
                        class="btn btn--secondary"
                        :disabled="!activeSkill"
                        @click="toggleCriteriaPicker"
                      >
                        + {{ t("pages.examDetail.add") }}
                      </button>
                    </div>
                    <div
                      v-if="showCriteriaPicker"
                      class="picker"
                      @mouseleave="showCriteriaPicker = false"
                    >
                      <div class="picker__title">
                        Critères disponibles
                      </div>

                      <div class="picker__list">
                        <div
                          v-for="c in availableCriteria[activeSkillId] ?? []"
                          :key="c.id"
                          class="picker__item"
                        >
                          <span>{{ c.label }}</span>

                          <button
                            type="button"
                            class="btn btn--secondary"
                            @click="addExistingCriterion(activeSkillId, c)"
                          >
                            +
                          </button>
                        </div>

                        <div v-if="(availableCriteria[activeSkillId] ?? []).length === 0">
                          Aucun critère disponible
                        </div>
                      </div>
                    </div>


                    <div v-if="!activeSkill" class="hint">
                      {{ t("pages.examDetail.chooseSkillHint") }}
                    </div>

                    <div v-else class="criteria-list">
                      <div
                        v-for="c in (criteria[activeSkillId] ?? [])"
                        :key="c.id"
                        class="criterion-block"
                      >
                        <div class="criterion-main">

                          <select
                            v-model="c.valuePreset"
                            class="criterion-total"
                            :disabled="isReadOnly"
                            @change="
                              c.valuePreset !== 'other'
                                ? (c.totalValue = Number(c.valuePreset))
                                : (c.totalValue = 0)
                            "
                          >
                            <option disabled value="0">{{ t("pages.examDetail.criterionValue") }}</option>
                            <option
                              v-for="v in [5,10,15,20,25,30]"
                              :key="v"
                              :value="v"
                            >
                              {{ v }}
                            </option>
                            <option value="other">{{ t("pages.examDetail.criterionOther") }}</option>
                          </select>

                          <input
                            v-if="c.valuePreset === 'other'"
                            v-model.number="c.totalValue"
                            type="number"
                            class="criterion-total"
                            :placeholder="t('pages.examDetail.criterionCustomValue')"
                            :disabled="isReadOnly"
                          />

                          <input
                            v-model="c.text"
                            class="criterion-name"
                            :placeholder="t('pages.examDetail.criterionName')"
                            :disabled="isReadOnly"
                            readonly
                          />

                          <button
                            v-if="!isReadOnly"
                            class="criterion-x"
                            @click="removeCriterion(activeSkillId, c.id)"
                          >
                            ✕
                          </button>
                        </div>

                        <div
                          v-for="e in c.evaluations"
                          :key="e.weight"
                          class="criterion-weight-row"
                          :class="{ 'criterion-weight-row--disabled': !e.enabled }"
                        >
                          <label class="weight-letter">
                            <input
                              type="checkbox"
                              v-model="e.enabled"
                              :disabled="isReadOnly || e.weight === 'A'"
                            />
                            {{ e.weight }}
                          </label>

                          <input
                            v-model.number="e.value"
                            type="number"
                            class="weight-value"
                            :min="0"
                            :placeholder="e.value === 0 ? t('pages.examDetail.weightValue') : ''"
                            :disabled="isReadOnly || !e.enabled || !c.totalValue"
                            @input="clampWeightValue(c, e)"
                          />

                          <input
                            v-model="e.description"
                            class="weight-description"
                            :placeholder="t('pages.examDetail.weightDescription')"
                            :disabled="isReadOnly || !e.enabled"
                          />

                          <div class="weight-percent">
                            {{ weightPercentage(c, e) }}%
                          </div>
                        </div>
                      </div>

                      <div
                        v-if="(criteria[activeSkillId] ?? []).length === 0"
                        class="hint"
                      >
                        {{ t("pages.examDetail.noCriteria") }}
                      </div>
                    </div>
                  </div>

                  <div class="description-right">
                    <div class="donut" :style="{ '--p': progress }">
                      <div class="donut__inner">{{ progress }}%</div>
                    </div>
                    <div class="donut-hint">{{ t("pages.examDetail.completion") }}</div>
                  </div>

                </div>
              </Card>
            </div>
          </div>

          <!-- =========================
          COLONNE DROITE (NOUVELLE)
          ========================== -->
          <div v-if="showSidePanel" class="info-modal__side">
            <h3 class="side-title">{{ t("pages.examDetail.skillsDistribution") }}</h3>

            <svg class="skills-donut" viewBox="0 0 120 120">
              <circle
                cx="60"
                cy="60"
                r="52"
                class="donut-bg"
              />

              <circle
                v-for="(s, i) in selectedSkills"
                :key="s.id"
                cx="60"
                cy="60"
                r="52"
                class="donut-segment"
                :stroke="donutColors[i % donutColors.length]"
                :stroke-dasharray="dashArray(s.id)"
                :stroke-dashoffset="dashOffset(s.id)"
              />
            </svg>

            <div class="donut-legend">
              <div
                v-for="(s, i) in selectedSkills"
                :key="s.id"
                class="legend-item"
              >
                <span
                  class="legend-dot"
                  :style="{ background: donutColors[i % donutColors.length] }"
                />
                <span class="legend-label">{{ s.label }}</span>
                <span class="legend-value">
                  {{ skillTotal(s.id) }} {{ t("pages.examDetail.pointsUnit") }} · {{ skillPercent(s.id) }}{{ t("pages.examDetail.percentUnit") }}
                </span>
              </div>
            </div>
          </div>

        </div>
      </div>
    </FullScreenModal>

    <div class="evaluation__preview">
      <div v-if="!isReadOnly" class="evaluation__preview-header">
        <h2 class="evaluation__preview-title">
          {{ examTitle }}
        </h2>
      </div>

      <div
        v-for="comp in previewCompetencies"
        :key="comp.id"
        class="evaluation__preview-comp"
      >
        <table class="evaluation__preview-table">
          <thead>
            <tr>
              <th class="evaluation__preview-th evaluation__preview-th--label">
                <span
                  class="evaluation__color-dot"
                  :style="{ backgroundColor: comp.color }"
                ></span>
                {{ comp.name }}
              </th>

              <th
                v-for="grade in previewGrades"
                :key="grade"
                class="evaluation__preview-th"
                :class="`evaluation__preview-th--${grade}`"
              >
                {{ grade }}
              </th>

            </tr>
          </thead>

          <tbody>
            <tr v-for="crit in comp.criteria" :key="crit.id">
              <td class="evaluation__preview-td evaluation__preview-td--label">
                {{ crit.label }}
              </td>

              <td
                v-for="grade in previewGrades"
                :key="grade"
                class="evaluation__preview-td"
              >
                <div class="evaluation__preview-desc">
                  {{ crit.descriptions[grade] ?? "" }}
                </div>

                <div
                  class="evaluation__preview-pts"
                  v-if="crit.weights[grade] != null && crit.enabled[grade]"
                >
                  {{ crit.weights[grade] }}%
                </div>
              </td>
            </tr>

            <tr class="evaluation__preview-summary">
              <td class="evaluation__preview-td evaluation__preview-td--label">
                {{ t("evaluation.competencyGrade") }}
              </td>

              <td
                v-for="grade in previewGrades"
                :key="grade"
                class="evaluation__preview-td evaluation__preview-td--summary"
              >
                —
              </td>
            </tr>
          </tbody>
        </table>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted , ref, watch, nextTick } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import { useProgramService } from "@/inversify.config";
import FullScreenModal from "@/components/popups/FullScreenModal.vue";
import ConfirmResetPopup from "@/components/popups/ConfirmResetPopup.vue";
import Card from "@/components/layouts/items/Card.vue";
const { t } = useI18n();
const route = useRoute();
const classesStore = useClassesStore();
const programService = useProgramService();
const showCriteriaPicker = ref(false);
const availableCriteria = ref<Record<string, any[]>>({});

const isReadOnly = computed(() => route.query.readOnly === '1');
const classId = computed(() => String(route.params.classId ?? ""));
const examId = computed(() => String(route.params.examId ?? ""));


function normalizeSkill(s: any): Skill {
  const rawExamSkillId = s?.examSkillId ?? s?.ExamSkillId ?? s?.id ?? s?.Id ?? "";
  const rawId = s?.skillId ?? s?.SkillId ?? s?.id ?? s?.Id;

  return {
    id: String(rawId),
    label: String(s?.label ?? s?.Label ?? ""),
    examSkillId: String(rawExamSkillId),
  };
}
const fetchedExamName = ref("");
const exam = computed(() => {
  return classesStore.getExamsForClass(classId.value)?.find((e) => e.id === examId.value);
});
const examTitle = computed(() => String(fetchedExamName.value || exam.value?.name || route.query.examName || t("pages.examDetail.titleFallback")));

const showInfo = ref(false);
const showResetConfirm = ref(false);

/* =========================
DONUT COMPETENCES
========================= */

const donutColors = [
  "#4655a0",
  "#6aa6ff",
  "#5cc6a7",
  "#f2b705",
  "#e56b6f",
];

const RADIUS = 52;
const CIRCUMFERENCE = 2 * Math.PI * RADIUS;

function skillTotal(skillId: string) {
  return (criteria.value[skillId] ?? []).reduce(
    (sum, c) => sum + (c.totalValue || 0),
    0
  );
}

const totalAllSkills = computed(() =>
  selectedSkills.value.reduce(
    (sum, s) => sum + skillTotal(s.id),
    0
  )
);

function skillPercent(skillId: string) {
  if (!totalAllSkills.value) return 0;
  return Math.round((skillTotal(skillId) / totalAllSkills.value) * 100);
}

function dashArray(skillId: string) {
  const percent = skillPercent(skillId) / 100;
  return `${percent * CIRCUMFERENCE} ${CIRCUMFERENCE}`;
}

function dashOffset(skillId: string) {
  let offsetPercent = 0;

  for (const s of selectedSkills.value) {
    if (s.id === skillId) break;
    offsetPercent += skillPercent(s.id);
  }

  return -(offsetPercent / 100) * CIRCUMFERENCE;
}

type Skill = { id: string; label: string; examSkillId: string };

const FALLBACK_WEIGHTS = ["A", "B", "C", "D", "E", "F"];
type WeightKey = string;

type DefaultLetter = {
  letter: WeightKey;
  description: string;
  defaultPercent: number;
  isEnabled: boolean;
};

async function loadAvailableCriteria(skillId: string) {
  console.log("loadAvailableCriteria appelé avec:", skillId);
  if (!skillId) return;

  const res = await fetch(`/api/skills/${skillId}/subcompetencies`);
  const all = await res.json();

  console.log("résultat API:", all);

  const existing = new Set(
    (criteria.value[skillId] ?? []).map(c => c.text)
  );

  availableCriteria.value[skillId] = all.filter(
    (c: any) => !existing.has(c.label)
  );
}

function toggleCriteriaPicker() {
  if (!activeSkillId.value) return;
  showCriteriaPicker.value = !showCriteriaPicker.value;
  if (showCriteriaPicker.value) {
    loadAvailableCriteria(activeSkillId.value);
  }
}

function addExistingCriterion(skillId: string, c: any) {
  const id =
    crypto.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;

  const evaluations = weights.value.map((w) => {
    const found = defaultLetters.value.find(x => x.letter === w);

    return {
      weight: w,
      value: 0,
      description: found?.description ?? "",
      enabled: found?.isEnabled ?? (w === "A"),
    };
  });

  criteria.value[skillId].push({
    id,
    text: c.label,
    totalValue: c.totalValue ?? 0,
    valuePreset: c.totalValue ?? 0,
    evaluations,
  });

  availableCriteria.value[skillId] =
    availableCriteria.value[skillId].filter(x => x.label !== c.label);
}

const defaultLetters = ref<DefaultLetter[]>([]);
async function loadDefaultLetters() {
  const res = await fetch("/api/default-criterion-letters");
  if (!res.ok) {
    throw new Error("Impossible de charger les lettres par défaut");
  }

  const data = await res.json();

  defaultLetters.value = (data as any[]).map((x) => ({
    letter: String(x.letter) as WeightKey,
    description: String(x.description ?? ""),
    defaultPercent: Number(x.defaultPercent ?? 0),
    isEnabled: !!x.isEnabled,
  }));
}


type WeightEvaluation = {
  weight: WeightKey;
  value: number;
  description: string;
  enabled: boolean;
};
type Criterion = {
  id: string;
  text: string;
  totalValue: number;
  valuePreset: number | "other";
  evaluations: WeightEvaluation[];
};

function clampWeightValue(c: Criterion, e: WeightEvaluation) {
  if (!c.totalValue) {
    e.value = 0;
    return;
  }

  if (e.value < 0) e.value = 0;
  if (e.value > c.totalValue) e.value = c.totalValue;
}

function weightPercentage(c: Criterion, e: WeightEvaluation) {
  if (!c.totalValue || !e.value) return 0;

  const percent = (e.value / c.totalValue) * 100;

  return Number(percent.toFixed(1));
}

const weights = computed<WeightKey[]>(() => {
  const configured = defaultLetters.value
    .map((x) => String(x.letter ?? "").trim())
    .filter((x) => x.length > 0);
  return configured.length > 0 ? configured : FALLBACK_WEIGHTS;
});

const previewGrades = computed(() => [...weights.value].reverse());

const previewCompetencies = computed(() =>
  selectedSkills.value.map((skill, index) => {
    const skillCriteria = criteria.value[skill.id] ?? [];

    return {
      id: skill.id,
      name: skill.label,
      color: donutColors[index % donutColors.length],
      criteria: skillCriteria.map((c) => {
        const descriptions: Partial<Record<WeightKey, string>> = {};
        const weights: Partial<Record<WeightKey, number>> = {};
        const enabled: Partial<Record<WeightKey, boolean>> = {};

        for (const e of c.evaluations) {
          descriptions[e.weight] = e.description ?? "";
          weights[e.weight] = weightPercentage(c, e);
          enabled[e.weight] = !!e.enabled;
        }

        return {
          id: c.id,
          label: c.text,
          descriptions,
          weights,
          enabled,
        };
      }),
    };
  })
);

function computeDefaultWeightValue(totalValue: number, percent: number) {
  if (!totalValue || totalValue <= 0) return 0;

  return Number(((percent / 100) * totalValue).toFixed(2));
}

const prevTotalValues = new Map<string, number>();

function recalculateCriterionValues(c: Criterion) {
  const prevTotal = prevTotalValues.get(c.id) ?? 0;
  const totalChanged = c.totalValue !== prevTotal;
  prevTotalValues.set(c.id, c.totalValue);

  for (const e of c.evaluations) {
    const def = defaultLetters.value.find(x => x.letter === e.weight);
    const percent = def?.defaultPercent ?? 0;

    if (!e.enabled || !c.totalValue) {
      e.value = 0;
      continue;
    }

    if (e.value === 0 || totalChanged) {
      e.value = computeDefaultWeightValue(c.totalValue, percent);
    }
  }
}


const showSidePanel = ref(false);

const allSkills = ref<Skill[]>([]);
const defaultSelectedSkillIds = computed(() => [] as string[]);
const isResetting = ref(false);

async function applyDefaultSkillsToExam() {
  if (!examId.value) return;
  if (defaultSelectedSkillIds.value.length === 0) return;

  const defaultsToAdd = allSkills.value.filter(s =>
    defaultSelectedSkillIds.value.includes(String(s.id))
  );

  for (const skill of defaultsToAdd) {
    await fetch(`/api/exams/${examId.value}/skills`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ skillId: skill.id }),
    });

  }
}
async function loadAvailableSkills(): Promise<Skill[]> {
  await classesStore.fetchClasses();
  const currentClass = classesStore.getClasses.find((c) => c.id === classId.value);
  const pid = currentClass?.programId;

  if (pid) {
    const programSkills = await programService.getProgramSkills(pid);
    if (programSkills.length > 0) {
      return programSkills.map((s) => normalizeSkill({ id: s.id, label: s.label }));
    }
  }

  const res = await fetch("/api/skills");
  const data = await res.json();
  return (data as any[]).map(normalizeSkill);
}

onMounted(async () => {
  if (!examId.value) return;

  try {
    const groupId = route.params.groupId as string | undefined;
    const examsUrl = groupId
      ? `/api/classes/${classId.value}/groups/${groupId}/exams`
      : `/api/classes/${classId.value}/exams`;
    const examsRes = await fetch(examsUrl);
    if (examsRes.ok) {
      const examsData = await examsRes.json();
      const found = (examsData as any[]).find((e: any) => String(e.id) === examId.value);
      if (found) fetchedExamName.value = String(found.name);
    }
  } catch { }

  const [skillsList, defaultLettersRes, examSkillsRes] = await Promise.all([
    loadAvailableSkills(),
    fetch("/api/default-criterion-letters"),
    fetch(`/api/exams/${examId.value}/skills`),
  ]);

  const [defaultLettersData, examSkillsData] = await Promise.all([
    defaultLettersRes.json(),
    examSkillsRes.json(),
  ]);

  allSkills.value = skillsList;

  defaultLetters.value = (defaultLettersData as any[]).map((x) => ({
    letter: String(x.letter) as WeightKey,
    description: String(x.description ?? ""),
    defaultPercent: Number(x.defaultPercent ?? 0),
    isEnabled: !!x.isEnabled,
  }));

  let examSkillRows = (examSkillsData as any[]).map(normalizeSkill);

  if (!isReadOnly.value && examSkillRows.length === 0 && defaultSelectedSkillIds.value.length > 0) {
    await applyDefaultSkillsToExam();

    const reloadExamSkillsRes = await fetch(`/api/exams/${examId.value}/skills`);
    const reloadExamSkillsData = await reloadExamSkillsRes.json();
    examSkillRows = (reloadExamSkillsData as any[]).map(normalizeSkill);
  }

  const seen = new Set<string>();
  selectedSkills.value = examSkillRows.filter(s => {
    if (seen.has(s.id)) return false;
    seen.add(s.id);
    return true;
  });

  for (const s of selectedSkills.value) {
    criteria.value[s.id] ??= [];
  }

  activeSkillId.value = selectedSkills.value[0]?.id ?? "";

  await Promise.all(
    selectedSkills.value.map(async (skill) => {
      criteria.value[skill.id] = await fetchCriteriaForSkill(skill.id);
    })
  );
});

const showPicker = ref(false);

const selectedSkills = ref<Skill[]>([]);
const activeSkillId = ref<string>("");

const criteria = ref<Record<string, Criterion[]>>({});

const activeSkill = computed(() => selectedSkills.value.find(s => s.id === activeSkillId.value) ?? null);

async function fetchCriteriaForSkill(skillId: string) {
  const res = await fetch(`/api/exams/${examId.value}/skills/${skillId}/criteria`);
  const data = await res.json();

  return data.map((c: any) => ({
    id: c.id,
    text: c.label,
    totalValue: c.totalValue,
    valuePreset: [5, 10, 15, 20, 25, 30].includes(c.totalValue) ? c.totalValue : "other",
    evaluations: weights.value.map((w) => {
      const found = c.weights.find((x: any) => x.weight === w);
      const def = defaultLetters.value.find(x => x.letter === w);

      return {
        weight: w,
        value: found?.value ?? 0,
        description: found?.description ?? def?.description ?? "",
        enabled: found?.isEnabled ?? def?.isEnabled ?? (w === "A"),
      };
    }),
  }));
}

function isSelected(id: string | number) {
  return selectedSkills.value.some(s => s.id === String(id));
}

watch(selectedSkills, (list) => {
  if (!list.some(s => s.id === activeSkillId.value)) {
    activeSkillId.value = list[0]?.id ?? "";
  }
});

async function toggleSkill(skill: Skill) {
  if (!examId.value) return;

  const normalizedSkill = normalizeSkill(skill);
  const alreadySelected = isSelected(normalizedSkill.id);

  if (alreadySelected) {
    await fetch(
      `/api/exams/${examId.value}/skills/${skill.id}`,
      { method: "DELETE" }
    );

    selectedSkills.value = selectedSkills.value.filter(s => s.id !== normalizedSkill.id);
    delete criteria.value[normalizedSkill.id];

    if (activeSkillId.value === normalizedSkill.id) {
      activeSkillId.value = selectedSkills.value[0]?.id ?? "";
    }
  } else {

    if (isSelected(normalizedSkill.id)) return;

    await fetch(`/api/exams/${examId.value}/skills`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ skillId: normalizedSkill.id }),
    });

    selectedSkills.value.push(normalizedSkill);
    criteria.value[normalizedSkill.id] ??= [];

    activeSkillId.value ||= skill.id;

  }
}

async function resetDefaults() {
  if (!examId.value || isResetting.value) return;
  isResetting.value = true;

  try {
    const [skillsList, defaultLettersRes] = await Promise.all([
      loadAvailableSkills(),
      fetch("/api/default-criterion-letters"),
    ]);

    const defaultLettersData = await defaultLettersRes.json();

    allSkills.value = skillsList;

    defaultLetters.value = (defaultLettersData as any[]).map((x: any) => ({
      letter: String(x.letter) as WeightKey,
      description: String(x.description ?? ""),
      defaultPercent: Number(x.defaultPercent ?? 0),
      isEnabled: !!x.isEnabled,
    }));

    const currentRes = await fetch(`/api/exams/${examId.value}/skills`);
    const currentSkills = await currentRes.json();
    for (const raw of currentSkills as any[]) {
      const normalized = normalizeSkill(raw);
      await fetch(
        `/api/exams/${examId.value}/skills/${normalized.id}`,
        { method: "DELETE" }
      );
    }

    const defaultsToAdd = allSkills.value.filter((s) =>
      defaultSelectedSkillIds.value.includes(String(s.id))
    );

    for (const skill of defaultsToAdd) {
      await fetch(`/api/exams/${examId.value}/skills`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify({ skillId: skill.id }),
      });
    }

    const afterRes = await fetch(`/api/exams/${examId.value}/skills`);
    const afterSkillsData = await afterRes.json();

    const seen = new Set<string>();
    selectedSkills.value = (afterSkillsData as any[])
      .map(normalizeSkill)
      .filter((s: any) => {
        if (seen.has(s.id)) return false;
        seen.add(s.id);
        return true;
      });

    criteria.value = {};
    for (const s of selectedSkills.value) {
      criteria.value[s.id] = [];
    }

    activeSkillId.value = selectedSkills.value[0]?.id ?? "";
    showPicker.value = false;
  } finally {
    isResetting.value = false;
  }
}

async function confirmResetDefaults() {
  await resetDefaults();
  showResetConfirm.value = false;
}

function addCriterion() {
  if (!activeSkillId.value) return;

  const id = crypto.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;

  const evaluations = weights.value.map((w) => {
    const found = defaultLetters.value.find(x => x.letter === w);

    return {
      weight: w,
      value: 0,
      description: found?.description ?? "",
      enabled: found?.isEnabled ?? (w === "A"),
    };
  });

  criteria.value[activeSkillId.value].push({
    id,
    text: "",
    totalValue: 0,
    valuePreset: 0,
    evaluations,
  });
}


const progress = computed(() => {
  if (!activeSkillId.value) return 0;
  const list = criteria.value[activeSkillId.value] ?? [];
  if (list.length === 0) return 0;
  const filled = list.filter(c => c.text.trim().length > 0).length;
  return Math.round((filled / list.length) * 100);
});

async function saveCriteria(skillId: string) {
  console.log("Sauvegarde critère pour", skillId);

  if (!examId.value) return;
  if (!criteria.value[skillId]) return;

  const examSkill = selectedSkills.value.find(s => s.id === skillId);
  if (!examSkill) return;

  await fetch(
    `/api/exams/${examId.value}/skills/${skillId}/criteria`,
    {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        examSkillId: examSkill.examSkillId || skillId,
        criteria: criteria.value[skillId]
          .filter(c => c.text.trim() && c.totalValue > 0)
          .map((c, i) => ({
            label: c.text,
            totalValue: c.totalValue,
            position: i,
            weights: c.evaluations
              .filter(e => e.enabled && e.value > 0)
              .map(e => ({
                weight: e.weight,
                value: e.value,
                description: e.description,
                isEnabled: true,
              })),
          })),
      }),
    }
  );
}

async function removeCriterion(skillId: string, criterionId: string) {
  if (!examId.value || !skillId) return;

   if (saveTimeout) {
    clearTimeout(saveTimeout);
    saveTimeout = undefined;
  }

  const list = criteria.value[skillId];
  if (!list) return;

  criteria.value[skillId] = list.filter(c => c.id !== criterionId);

  await fetch(
    `/api/exams/${examId.value}/skills/${skillId}/criteria/${criterionId}`,
    { method: "DELETE" }
  );

  await reloadCriteria(skillId);
}

let isReloading = false;

async function reloadCriteria(skillId: string) {
  if (!examId.value) return;
  isReloading = true;

  criteria.value[skillId] = await fetchCriteriaForSkill(skillId);
  await nextTick();
  await nextTick();
  isReloading = false;
}



let saveTimeout: number | undefined;

function isCriterionPayloadReady(skillId: string) {
  const list = criteria.value[skillId] ?? [];
  return list.every((c) => {
    const hasTotal = c.totalValue > 0;
    const hasWeight = c.evaluations.some((e) => e.enabled && e.value > 0);
    return hasTotal && hasWeight;
  });
}

function debounceSave(skillId: string) {
  if (saveTimeout) {
    clearTimeout(saveTimeout);
  }

  saveTimeout = window.setTimeout(() => {
    if (!isCriterionPayloadReady(skillId)) return;
    saveCriteria(skillId);
  }, 600);
}

watch(
  criteria,
  () => {
    if (isReloading) return;
    if (!activeSkillId.value) return;
    if (isReadOnly.value) return;

    const list = criteria.value[activeSkillId.value] ?? [];
    for (const c of list) {
      recalculateCriterionValues(c);
    }

    debounceSave(activeSkillId.value);
  },
  { deep: true }
);

watch(activeSkillId, (skillId) => {
  if (skillId) reloadCriteria(skillId);
});


</script>

<style scoped>
/* =========================
BOUTON RETOUR
========================= */

.exam-detail__back {
  display: flex;
  align-items: center;
  gap: 12px;
}

.exam-detail__back-link {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 36px;
  border-radius: 8px;
  font-size: 20px;
  color: rgba(0, 0, 0, 0.5);
  text-decoration: none;
  transition: background-color 0.2s, color 0.2s;
}

.exam-detail__back-link:hover {
  background-color: rgba(0, 0, 0, 0.08);
  color: rgba(0, 0, 0, 0.8);
}

/* =========================
MODAL / TITRES
========================= */

.info-modal {
  padding: 24px;
}

.info-modal__section-title {
  margin: 0 0 14px;
  font-size: 22px;
  font-weight: 900;

}


/* =========================
ACTION HAUT DROITE
========================= */

.info-modal__top-actions {
  display: flex;
  justify-content: flex-end;
  margin-bottom: 40px;
  border-bottom: 2px solid rgba(184, 160, 136, 0.3);
  padding-bottom: 20px;
}

.btn--reset {
  margin-right: 8px;
  border: 2px solid #e53935;
  color: #e53935;
  background: transparent;
  font-weight: 800;
}

.btn--reset:hover {
  background: rgba(229, 57, 53, 0.08);
}

.btn--reset:disabled {
  opacity: 0.6;
  cursor: not-allowed;
}

/* =========================
LAYOUT PRINCIPAL
========================= */

.info-modal__layout {
  display: flex;
  align-items: stretch;
  gap: 16px;
}

.info-modal__main {
  flex: 1;
  min-width: 0;
  display: flex;
  flex-direction: column;
}

.info-modal__side {
  width: 320px;
  padding: 16px;
  border-radius: 14px;
  border: 2px solid rgba(184, 160, 136, 0.3);
  background: rgba(232, 221, 208, 0.2);
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.side-title {
  margin: 10px auto;
  font-size: 18px;
  font-weight: 900;
}

.side-hint {
  opacity: 0.75;
  font-size: 14px;
}

.side-list {
  padding-left: 18px;
  font-size: 14px;
  font-weight: 600;
}

/* =========================
COMPÉTENCES
========================= */

.skills-grid {
  display: flex;
  flex-direction: column;
  gap: 12px;
  margin-bottom: 12px;
}

.skills-picker {
  position: relative;
}

.skills-picker .picker {
  position: absolute;
  top: 100%;
  left: 0;
  right: 0;
  z-index: 20;
}

.skills-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(240px, 1fr));
  gap: 12px;
  margin-bottom: 40px;
  border-bottom: 2px solid rgba(184, 160, 136, 0.3);
  padding-bottom: 40px;
}

.skill-card {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 16px;
  border-radius: 14px;
  border: 2px solid rgba(184, 160, 136, 0.4);
  background: rgba(232, 221, 208, 0.2);
  cursor: pointer;
  text-align: left;
}

.skill-card--active {
  border-color: #b8a088;
  box-shadow: rgba(184, 160, 136, 0.25) inset;
}

.skill-card__dot {
  margin-top: 2px;
  width: 12px;
  height: 12px;
  border-radius: 999px;
  border: 3px solid #1a1a2e;
}

.skill-card__label {
  font-weight: 800;
  line-height: 1.15;
}

.skill-card--add {
  align-items: center;
  gap: 12px;
  width: 100%;
  justify-content: center;
}

.skill-card__plus {
  width: 34px;
  height: 34px;
  border-radius: 999px;
  display: grid;
  place-items: center;
  font-size: 22px;
  font-weight: 900;
  background: rgba(184, 160, 136, 0.3);
}

/* =========================
DONUT COMPETENCES
========================= */

.skills-donut {
  width: 220px;
  height: 220px;
  margin: 50px auto;
  transform: rotate(-90deg);
}

.donut-bg {
  fill: none;
  stroke: #e6e6e6;
  stroke-width: 12;
}

.donut-segment {
  fill: none;
  stroke-width: 12;
  transition: stroke-dasharray 0.3s ease, stroke-dashoffset 0.3s ease;
}

.donut-legend {
  display: flex;
  flex-direction: column;
  gap: 8px;
  margin-top: 12px;
}

.legend-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 700;
  font-size: 13px;
}

.legend-dot {
  width: 10px;
  height: 10px;
  border-radius: 999px;
}

.legend-label {
  flex: 1;
}

.legend-value {
  opacity: 0.7;
  font-weight: 800;
}

/* =========================
PICKER COMPÉTENCES
========================= */

.picker {
  margin-bottom: 18px;
  padding: 14px;
  border-radius: 14px;
  border: 2px solid rgba(184, 160, 136, 0.3);
  background: #fff;
  box-shadow: 0 12px 30px rgba(26, 26, 46, 0.12);
}

.picker__title {
  font-weight: 900;
  margin-bottom: 10px;
}

.picker__list {
  display: grid;
  gap: 10px;
}

.picker__item {
  display: flex;
  gap: 10px;
  align-items: center;
  padding: 10px 12px;
  border-radius: 12px;
  background: rgba(232, 221, 208, 0.25);
}

/* =========================
DESCRIPTION
========================= */


.description-card__content {
  display: flex;
  gap: 18px;
}

.description-section {
  margin-top: auto;
}

.description-left {
  flex: 1;
  padding: 18px;
}

.description-title {
  margin: 0 0 10px;
  font-size: 22px;
  font-weight: 900;
}

/* =========================
CRITÈRES
========================= */

.criteria-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 12px;
  margin: 10px 0;
}

.criteria-title {
  font-weight: 900;
}

.criteria-list {
  display: grid;
  gap: 12px;
}

.criterion-block {
  border: 1px solid rgba(184, 160, 136, 0.35);
  border-radius: 12px;
  padding: 14px;
  background: #fff;
}

.criterion-main {
  display: flex;
  gap: 10px;
  align-items: center;
  margin-bottom: 10px;
}

.criterion-total {
  width: 185px;
  padding: 8px 10px;
  text-align: center;
  border-radius: 12px;
  border: 2px solid rgba(184, 160, 136, 0.4);
  background: #f9fafb;
  font-weight: 800;
}

.criterion-name {
  flex: 1;
  font-weight: 700;
}

.criterion-x {
  width: 38px;
  height: 38px;
  border-radius: 12px;
  border: 0;
  background: rgba(184, 160, 136, 0.2);
  cursor: pointer;
  font-weight: 900;
}

/* =========================
WEIGHTS (ABCDE)
========================= */

.criterion-weight-row {
  display: grid;
  grid-template-columns: 48px 120px 1fr 48px;
  gap: 10px;
  margin-left: 10px;
  margin-bottom: 6px;
  transition: opacity 0.2s ease;
}

.weight-letter {
  display: flex;
  align-items: center;
  gap: 6px;
  font-weight: 900;
}

.weight-value {
  text-align: center;
}

.weight-description {
  width: 100%;
}

.weight-percent {
  width: 48px;
  text-align: right;
  font-weight: 800;
  opacity: 0.75;
  font-size: 13px;
}

.criterion-weight-row--disabled {
  opacity: 0.35;
}

.criterion-weight-row--disabled input {
  pointer-events: none;
}

.hint {
  opacity: 0.75;
  font-weight: 600;
  padding: 6px 0;
}

/* =========================
DONUT - COMPLETION
========================= */

.description-right {
  width: 210px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-direction: column;
  gap: 10px;
  padding: 18px;
}

.donut {
  --p: 0;
  width: 170px;
  height: 170px;
  border-radius: 999px;
  background: conic-gradient(
    rgba(70, 85, 160, 0.95) calc(var(--p) * 1%),
    rgba(0, 0, 0, 0.12) 0
  );
  display: grid;
  place-items: center;
}

.donut__inner {
  width: 120px;
  height: 120px;
  border-radius: 999px;
  background: #f0f0f0;
  display: grid;
  place-items: center;
  font-weight: 900;
  font-size: 24px;
}

.donut-hint {
  font-weight: 800;
  opacity: 0.8;
}

/* =========================
RESPONSIVE
========================= */

@media (max-width: 900px) {
  .skills-grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
  }

  .description-card__content {
    flex-direction: column;
  }

  .description-right {
    width: 100%;
  }

  .criterion-weight-row {
    margin-left: 0;
  }

  .info-modal,
  .info-modal--expanded {
    width: 100%;
  }

  .info-modal__layout {
    flex-direction: column;
  }

  .info-modal__main,
  .info-modal__side {
    width: 100%;
  }
}
</style>

