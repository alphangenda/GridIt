<template>
  <div class="exam-detail">
    <div class="exam-detail__header">
      <div>
        <h1 class="exam-detail__title">{{ exam?.name ?? t("navigation.exam") }}</h1>
        <p class="exam-detail__hint">{{ t("navigation.examDetailPlaceholder") }}</p>
      </div>

      <div class="exam-detail__actions">
        <button type="button" class="btn btn--secondary" @click="showInfo = true">
          Infos
        </button>
      </div>
    </div>

    <FullScreenModal v-model="showInfo" :class="{ 'modal--expanded': showSidePanel }">
      <div
        class="info-modal"
        :class="{ 'info-modal--expanded': showSidePanel }"
      >

        <div class="info-modal__top-actions">
          <button
            type="button"
            class="btn btn--secondary"
            @click="showSidePanel = !showSidePanel"
          >
            {{ showSidePanel ? 'Fermer' : 'Ouvrir' }} statistiques
          </button>
        </div>

        <div class="info-modal__layout">

          <!-- =========================
          COLONNE GAUCHE (INCHANGÉE)
          ========================== -->
          <div class="info-modal__main">

            <h2 class="info-modal__section-title">Compétences</h2>

            <div class="skills-grid">
              <button
                type="button"
                class="skill-card skill-card--add"
                @click="showPicker = !showPicker"
              >
                <span class="skill-card__plus">+</span>
                <span class="skill-card__label">Choisissez des compétences</span>
              </button>

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

            <div v-if="showPicker" class="picker">
              <div class="picker__title">Sélectionner</div>
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

            <h2 class="info-modal__section-title">Description</h2>

            <Card class="description-card">
              <div class="description-card__content">

                <div class="description-left">
                  <h3 class="description-title">
                    {{ activeSkill?.label ?? "Aucune compétence sélectionnée" }}
                  </h3>

                  <div class="criteria-header">
                    <div class="criteria-title">Critères</div>
                    <button
                      type="button"
                      class="btn btn--secondary"
                      :disabled="!activeSkill"
                      @click="addCriterion"
                    >
                      + Ajouter
                    </button>
                  </div>

                  <div v-if="!activeSkill" class="hint">
                    Choisis une compétence pour ajouter des critères.
                  </div>

                  <div v-else class="criteria-list">
                    <div
                      v-for="c in criteria[activeSkillId]"
                      :key="c.id"
                      class="criterion-block"
                    >
                      <div class="criterion-main">

                        <select
                          v-model="c.valuePreset"
                          class="criterion-total"
                          @change="
                            c.valuePreset !== 'other'
                              ? (c.totalValue = Number(c.valuePreset))
                              : (c.totalValue = 0)
                          "
                        >
                          <option disabled value="0">Valeur du critère</option>
                          <option
                            v-for="v in [5,10,15,20,25,30]"
                            :key="v"
                            :value="v"
                          >
                            {{ v }}
                          </option>
                          <option value="other">Autre</option>
                        </select>

                        <input
                          v-if="c.valuePreset === 'other'"
                          v-model.number="c.totalValue"
                          type="number"
                          class="criterion-total"
                          placeholder="Valeur personnalisée"
                        />

                        <input
                          v-model="c.text"
                          class="criterion-name"
                          placeholder="Nom du critère"
                        />

                        <button
                          class="criterion-x"
                          @click="removeCriterion(c.id)"
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
                            :disabled="e.weight === 'A'"
                          />
                          {{ e.weight }}
                        </label>

                        <input
                          v-model.number="e.value"
                          type="number"
                          class="weight-value"
                          :min="0"
                          :max="c.totalValue - sumOtherWeights(c, e)"
                          :placeholder="e.value === 0 ? 'Valeur du poids' : ''"
                          :disabled="!e.enabled || !c.totalValue"
                          @input="clampWeightValue(c, e)"
                        />

                        <input
                          v-model="e.description"
                          class="weight-description"
                          placeholder="Description"
                          :disabled="!e.enabled"
                        />

                        <div class="weight-percent">
                          {{ weightPercentage(c, e) }}%
                        </div>
                      </div>
                    </div>

                    <div
                      v-if="criteria[activeSkillId].length === 0"
                      class="hint"
                    >
                      Aucun critère pour cette compétence.
                    </div>
                  </div>
                </div>

                <div class="description-right">
                  <div class="donut" :style="{ '--p': progress }">
                    <div class="donut__inner">{{ progress }}%</div>
                  </div>
                  <div class="donut-hint">Complétion</div>
                </div>

              </div>
            </Card>
          </div>

          <!-- =========================
          COLONNE DROITE (NOUVELLE)
          ========================== -->
          <div v-if="showSidePanel" class="info-modal__side">
            <h3 class="side-title">Répartition des compétences</h3>

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
                  {{ skillTotal(s.id) }} pts · {{ skillPercent(s.id) }}%
                </span>
              </div>
            </div>
          </div>

        </div>
      </div>
    </FullScreenModal>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted , ref } from "vue";
import { useRoute } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import FullScreenModal from "@/components/popups/FullScreenModal.vue";
import Card from "@/components/layouts/items/Card.vue";
const { t } = useI18n();
const route = useRoute();
const classesStore = useClassesStore();

const exam = computed(() => {
  const classId = route.params.classId as string;
  const examId = route.params.examId as string;
  return classesStore.getExamsForClass(classId).find((e) => e.id === examId);
});

const showInfo = ref(false);

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

type Skill = { id: string; label: string };

const WEIGHTS = ["A", "B", "C", "D", "E"] as const;
type WeightKey = typeof WEIGHTS[number];

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

function sumOtherWeights(c: Criterion, current: WeightEvaluation) {
  return c.evaluations
    .filter(e => e !== current && e.enabled)
    .reduce((sum, e) => sum + e.value, 0);
}

function clampWeightValue(c: Criterion, e: WeightEvaluation) {
  if (!c.totalValue) {
    e.value = 0;
    return;
  }

  if (e.value < 0) e.value = 0;

  const maxAllowed = c.totalValue - sumOtherWeights(c, e);

  if (e.value > maxAllowed) {
    e.value = Math.max(0, maxAllowed);
  }
}

function weightPercentage(c: Criterion, e: WeightEvaluation) {
  if (!c.totalValue || !e.value) return 0;
  return Math.round((e.value / c.totalValue) * 100);
}


const showSidePanel = ref(false);


const allSkills = ref<Skill[]>([]);

onMounted(async () => {
  const res = await fetch("/api/skills");

  const data = await res.json();

  allSkills.value = data.map((s: any) => ({
    id: s.id ?? s.Id,
    label: s.label ?? s.Label,
  }));

  console.log("SKILLS MAPPÉES :", allSkills.value);
});

const showPicker = ref(false);

const selectedSkills = ref<Skill[]>([]);
const activeSkillId = ref<string>("");

const criteria = ref<Record<string, Criterion[]>>({});

const activeSkill = computed(() => selectedSkills.value.find(s => s.id === activeSkillId.value) ?? null);

function isSelected(id: string) {
  return selectedSkills.value.some(s => s.id === id);
}

function toggleSkill(skill: Skill) {
  if (isSelected(skill.id)) {
    selectedSkills.value = selectedSkills.value.filter(s => s.id !== skill.id);

    if (activeSkillId.value === skill.id) activeSkillId.value = "";
  } else {
    selectedSkills.value.push(skill);
    if (!activeSkillId.value) activeSkillId.value = skill.id;
    if (!criteria.value[skill.id]) criteria.value[skill.id] = [];
  }

  if (!activeSkillId.value && selectedSkills.value.length > 0) {
    activeSkillId.value = selectedSkills.value[0].id;
  }
}

function addCriterion() {
  if (!activeSkillId.value) return;

  const id = crypto.randomUUID?.() ?? `${Date.now()}-${Math.random()}`;

  criteria.value[activeSkillId.value].push({
    id,
    text: "",
    totalValue: 0,
    valuePreset: 0,
    evaluations: WEIGHTS.map(w => ({
      weight: w,
      value: 0,
      description: "",
      enabled: w === "A",
    })),
  });
}

function removeCriterion(id: string) {
  if (!activeSkillId.value) return;
  criteria.value[activeSkillId.value] = criteria.value[activeSkillId.value].filter(c => c.id !== id);
}

const progress = computed(() => {
  if (!activeSkillId.value) return 0;
  const list = criteria.value[activeSkillId.value] ?? [];
  if (list.length === 0) return 0;
  const filled = list.filter(c => c.text.trim().length > 0).length;
  return Math.round((filled / list.length) * 100);
});
</script>

<style scoped>
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
  margin-bottom: 12px;
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
}

.info-modal__side {
  width: 320px;
  padding: 16px;
  border-radius: 14px;
  border: 2px solid rgba(0,0,0,0.12);
  background: rgba(0,0,0,0.04);
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
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 16px;
  margin-bottom: 12px;
}

.skill-card {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 16px;
  border-radius: 14px;
  border: 2px solid rgba(0, 0, 0, 0.2);
  background: rgba(0, 0, 0, 0.06);
  cursor: pointer;
  text-align: left;
}

.skill-card--active {
  border-color: rgba(70, 85, 160, 0.95);
  box-shadow: 0 0 0 2px rgba(70, 85, 160, 0.2) inset;
}

.skill-card__dot {
  margin-top: 2px;
  width: 12px;
  height: 12px;
  border-radius: 999px;
  border: 3px solid rgba(0, 0, 0, 0.85);
}

.skill-card__label {
  font-weight: 800;
  line-height: 1.15;
}

.skill-card--add {
  align-items: center;
  gap: 12px;
}

.skill-card__plus {
  width: 34px;
  height: 34px;
  border-radius: 999px;
  display: grid;
  place-items: center;
  font-size: 22px;
  font-weight: 900;
  background: rgba(0, 0, 0, 0.1);
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
  border: 2px solid rgba(0, 0, 0, 0.12);
  background: rgba(0, 0, 0, 0.04);
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
  background: rgba(255, 255, 255, 0.7);
}

/* =========================
DESCRIPTION
========================= */

.description-card__content {
  display: flex;
  gap: 18px;
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
  border: 1px solid rgba(0, 0, 0, 0.12);
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
  border: 2px solid rgba(0, 0, 0, 0.15);
  background: #fff;
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
  background: rgba(0, 0, 0, 0.1);
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

