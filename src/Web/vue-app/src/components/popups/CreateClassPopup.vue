<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="handleSubmit">
      <div class="popup__bg" @click="emit('close')"></div>
      <div class="popup__container">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("navigation.addClass") }}</p>
        </div>
        <div class="popup__content">
          <div class="popup__block">
            <div class="form__group">
              <label class="form__label" for="class-name">{{ t("navigation.className") }}</label>
              <input
                id="class-name"
                ref="inputRef"
                v-model="name"
                class="form__input"
                type="text"
                required
              />
            </div>

            <div class="form__group">
              <label class="form__label">
                {{ t("pages.classes.skills") }}
              </label>

              <select
                v-model="selectedSkillIds"
                class="skills-select"
                multiple
                size="8"
              >
                <option
                  v-for="skill in skills"
                  :key="skill.id"
                  :value="skill.id"
                >
                  {{ skill.label }}
                </option>
              </select>
              <p class="skills-hint">{{ t("pages.classes.skillsHint") ?? t("pages.classes.skills") }}</p>
            </div>


            <div class="form__submit">
              <button class="btn btn--fullscreen" type="submit">{{ t("global.add") }}</button>
              <button class="btn btn--fullscreen btn--red" type="button" @click="emit('close')">{{ t("global.cancel") }}</button>
            </div>
          </div>
        </div>
      </div>
    </form>
  </Transition>
</template>

<script lang="ts" setup>
import { ref, onMounted } from "vue";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";
import { useSessionsStore } from "@/stores/sessionsStore";
import { notifyError } from "@/notify";


type Skill = {
  id: string;
  label: string;
};

const emit = defineEmits<{
  (event: "close"): void;
}>();

const { t } = useI18n();
const classesStore = useClassesStore();
const sessionsStore = useSessionsStore();

const name = ref("");
const inputRef = ref<HTMLInputElement | null>(null);


const skills = ref<Skill[]>([]);
const selectedSkillIds = ref<string[]>([]);

onMounted(async () => {
  inputRef.value?.focus();
  await loadSkills();
});


async function loadSkills() {
  try {
    const res = await fetch("/api/skills");

    if (!res.ok) {
      throw new Error("Impossible de charger les compétences");
    }

    const data = await res.json();

    skills.value = (data as any[]).map((skill) => ({
      id: String(skill.id),
      label: String(skill.label),
    }));
  } catch (error) {
    console.error(error);
    notifyError(t("pages.classes.skillsLoadError"));
  }
}

async function handleSubmit() {
  const trimmed = name.value.trim();

  if (!trimmed) return;

  try {
    const createdClass = await classesStore.addClass(trimmed, selectedSkillIds.value);

    const selectedSession = sessionsStore.getSelectedSession;

    if (selectedSession?.id) {
      const updatedClassIds = [...(selectedSession.classIds ?? []), createdClass.id];

      await sessionsStore.updateSession(
        selectedSession.id,
        selectedSession.name ?? "",
        updatedClassIds
      );
    }

    emit("close");
  } catch (error) {
    console.error(error);
    notifyError(t("pages.classes.addError"));
  }
}
</script>

<style scoped lang="scss">
.fade-leave-active,
.fade-enter-active {
  transition: opacity 0.2s cubic-bezier(0.69, 0.33, 0.16, 0.97);
}

.fade-enter-to,
.fade-leave-from {
  opacity: 1;
}

.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.skills-select {
  width: 100%;
  min-height: 180px;
  padding: 10px;
  border: 1px solid #e4e4e4;
  border-radius: 12px;
  background: #f8f9fb;
  font-size: 15px;
}

.skills-select option {
  padding: 8px 10px;
  border-radius: 6px;
}

.skills-hint {
  margin-top: 6px;
  font-size: 13px;
  color: #6b7280;
}
</style>
