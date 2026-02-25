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

    <FullScreenModal v-model="showInfo">
      <div class="info-modal">
        <h2 class="info-modal__section-title">Compétences</h2>

        <div class="skills-grid">
          <button type="button" class="skill-card skill-card--add" @click="showPicker = !showPicker">
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
            <label v-for="s in ALL_SKILLS" :key="s.id" class="picker__item">
              <input type="checkbox" :checked="isSelected(s.id)" @change="toggleSkill(s)" />
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
                <button type="button" class="btn btn--secondary" :disabled="!activeSkill" @click="addCriterion">
                  + Ajouter
                </button>
              </div>

              <div v-if="!activeSkill" class="hint">
                Choisis une compétence pour ajouter des critères.
              </div>

              <div v-else class="criteria-list">
                <div v-for="c in criteria[activeSkillId]" :key="c.id" class="criterion-row">
                  <select v-model="c.weight" class="criterion-weight">
                    <option v-for="w in WEIGHTS" :key="w" :value="w">{{ w }}</option>
                  </select>

                  <input v-model="c.text" class="criterion-text" placeholder="Écris ton critère..." />

                  <button type="button" class="criterion-x" @click="removeCriterion(c.id)">✕</button>
                </div>

                <div v-if="criteria[activeSkillId].length === 0" class="hint">
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
    </FullScreenModal>
  </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
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

type Skill = { id: string; label: string };
type Criterion = { id: string; text: string; weight: string };

const WEIGHTS = ["A", "B", "C", "D", "E"] as const;

const ALL_SKILLS: Skill[] = [
  { id: "general", label: "Configuration et qualité générale" },
  { id: "django", label: "Administration Django" },
  { id: "ui", label: "Templates et interface utilisateur" },
  { id: "api", label: "API externe et fonctionnalités avancées" },
  { id: "forms", label: "Formulaires et validations" },
  { id: "views", label: "Vues principales et logique" },
];

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
  const id = (crypto.randomUUID?.() ?? `${Date.now()}-${Math.random()}`);
  criteria.value[activeSkillId.value].push({ id, text: "", weight: "A" });
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
.info-modal{ padding:24px; }
.info-modal__section-title{ margin:0 0 14px; font-size:22px; font-weight:900; }

.skills-grid{
  display:grid;
  grid-template-columns: repeat(3, minmax(0,1fr));
  gap:16px;
  margin-bottom: 12px;
}

.skill-card{
  display:flex;
  align-items:flex-start;
  gap:10px;
  padding:16px;
  border-radius:14px;
  border:2px solid rgba(0,0,0,0.20);
  background: rgba(0,0,0,0.06);
  cursor:pointer;
  text-align:left;
}

.skill-card--active{
  border-color: rgba(70,85,160,.95);
  box-shadow: 0 0 0 2px rgba(70,85,160,.20) inset;
}

.skill-card__dot{
  margin-top:2px;
  width:12px; height:12px;
  border-radius:999px;
  border:3px solid rgba(0,0,0,0.85);
}
.skill-card__label{ font-weight:800; line-height:1.15; }

.skill-card--add{
  align-items:center;
  gap:12px;
}
.skill-card__plus{
  width:34px; height:34px;
  border-radius:999px;
  display:grid;
  place-items:center;
  font-size:22px;
  font-weight:900;
  background: rgba(0,0,0,0.10);
}

.picker{
  margin-bottom: 18px;
  padding: 14px;
  border-radius: 14px;
  border: 2px solid rgba(0,0,0,0.12);
  background: rgba(0,0,0,0.04);
}
.picker__title{ font-weight:900; margin-bottom:10px; }
.picker__list{ display:grid; gap:10px; }
.picker__item{
  display:flex; gap:10px; align-items:center;
  padding:10px 12px;
  border-radius:12px;
  background: rgba(255,255,255,0.7);
}

.description-card__content{ display:flex; gap:18px; }
.description-left{ flex:1; padding:18px; }
.description-title{ margin:0 0 10px; font-size:22px; font-weight:900; }

.criteria-header{
  display:flex;
  justify-content:space-between;
  align-items:center;
  gap:12px;
  margin: 10px 0;
}
.criteria-title{ font-weight:900; }

.criteria-list{ display:grid; gap:10px; }

.criterion-row{
  display:flex;
  gap:10px;
  align-items:center;
  padding:10px;
  border-radius:14px;
  background: rgba(0,0,0,0.04);
  border: 2px solid rgba(0,0,0,0.10);
}
.criterion-weight{
  width:74px;
  padding:8px;
  border-radius:12px;
  border:2px solid rgba(0,0,0,0.15);
  background:#fff;
  font-weight:900;
}
.criterion-text{
  flex:1;
  padding:10px 12px;
  border-radius:12px;
  border:2px solid rgba(0,0,0,0.15);
  background:#fff;
}
.criterion-x{
  width:38px; height:38px;
  border-radius:12px;
  border:0;
  background: rgba(0,0,0,0.10);
  cursor:pointer;
  font-weight:900;
}

.hint{ opacity:.75; font-weight:600; padding: 6px 0; }

.description-right{
  width: 210px;
  display:flex;
  align-items:center;
  justify-content:center;
  flex-direction:column;
  gap:10px;
  padding:18px;
}

.donut{
  --p: 0;
  width:170px; height:170px;
  border-radius:999px;
  background: conic-gradient(rgba(70,85,160,.95) calc(var(--p) * 1%), rgba(0,0,0,0.12) 0);
  display:grid;
  place-items:center;
}
.donut__inner{
  width:120px; height:120px;
  border-radius:999px;
  background: #f0f0f0;
  display:grid;
  place-items:center;
  font-weight:900;
  font-size:24px;
}
.donut-hint{ font-weight:800; opacity:.8; }

@media (max-width: 900px){
  .skills-grid{ grid-template-columns: repeat(2, minmax(0,1fr)); }
  .description-card__content{ flex-direction:column; }
  .description-right{ width:100%; }
}
</style>

