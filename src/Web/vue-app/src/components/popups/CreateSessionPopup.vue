<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="handleSubmit">
      <div class="popup__bg" @click="emit('close')"></div>
      <div class="popup__container">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("navigation.addSession") }}</p>
        </div>
        <div class="popup__content">
          <div class="popup__block">
            <div class="form__group">
              <label class="form__label">{{ t("navigation.sessionSeason") }}</label>
              <div class="season-grid">
                <label class="season-option" :class="{ 'season-option--selected': season === 'Winter' }">
                  <input type="radio" name="session-season" value="Winter" v-model="season" />
                  <span>{{ t("navigation.seasons.winter") }}</span>
                </label>
                <label class="season-option" :class="{ 'season-option--selected': season === 'Summer' }">
                  <input type="radio" name="session-season" value="Summer" v-model="season" />
                  <span>{{ t("navigation.seasons.summer") }}</span>
                </label>
                <label class="season-option" :class="{ 'season-option--selected': season === 'Fall' }">
                  <input type="radio" name="session-season" value="Fall" v-model="season" />
                  <span>{{ t("navigation.seasons.fall") }}</span>
                </label>
              </div>
            </div>
            <div class="form__group">
              <label class="form__label" for="session-year">{{ t("navigation.sessionYear") }}</label>
              <input
                id="session-year"
                ref="inputRef"
                v-model.number="year"
                class="form__input"
                type="number"
                min="1900"
                max="2200"
                required
              />
            </div>
            <div v-if="classes.length" class="form__group">
              <label class="form__label" for="session-classes">{{ t("navigation.classes") }}</label>
              <select
                id="session-classes"
                v-model="selectedClassIds"
                class="form__input"
                multiple
                size="5"
              >
                <option v-for="classItem in classes" :key="classItem.id" :value="classItem.id">
                  {{ classItem.name }}
                </option>
              </select>
              <small class="form__help">{{ t("navigation.selectMultipleClasses") }}</small>
            </div>
            <div class="form__submit">
              <button class="btn btn--fullscreen btn--red" type="button" @click="emit('close')">{{ t("global.cancel") }}</button>
              <button class="btn btn--fullscreen" type="submit">{{ t("global.add") }}</button>
            </div>
          </div>
        </div>
      </div>
    </form>
  </Transition>
</template>

<script lang="ts" setup>
import { ref, onMounted, computed } from "vue";
import { useRouter } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useSessionsStore } from "@/stores/sessionsStore";
import { useClassesStore } from "@/stores/classesStore";
import { notifyError } from "@/notify";

const emit = defineEmits<{
  (event: "close"): void;
}>();

const router = useRouter();
const { t } = useI18n();
const sessionsStore = useSessionsStore();
const classesStore = useClassesStore();

const year = ref<number>(new Date().getFullYear());
const season = ref<"Winter" | "Summer" | "Fall">("Winter");
const selectedClassIds = ref<string[]>([]);
const inputRef = ref<HTMLInputElement | null>(null);
const classes = computed(() => classesStore.getClasses);

onMounted(() => {
  inputRef.value?.focus();
  classesStore.fetchClasses();
});

async function handleSubmit() {
  if (!year.value || year.value < 1900 || year.value > 2200) return;

  const seasonLabel =
    season.value === "Winter"
      ? t("navigation.seasons.winter")
      : season.value === "Summer"
        ? t("navigation.seasons.summer")
        : t("navigation.seasons.fall");

  const sessionName = `${seasonLabel} ${year.value}`;

  try {
    const created = await sessionsStore.addSession(
      sessionName,
      selectedClassIds.value
    );

    if (created?.id) {
      sessionsStore.selectSession(created.id);

      const firstClassId =
        (created.classIds && created.classIds[0]) ||
        (selectedClassIds.value && selectedClassIds.value[0]);

      if (firstClassId) {
        await router.push({
          name: "classes.detail",
          params: { classId: firstClassId },
        });
      } else {
        await router.push({
          name: "sessions.detail",
          params: { sessionId: created.id },
        });
      }
    }

    emit("close");
    // Reset form
    year.value = new Date().getFullYear();
    season.value = "Winter";
    selectedClassIds.value = [];
  } catch {
    notifyError(t("pages.sessions.addError"));
  }
}
</script>

<style scoped lang="scss">
.fade-leave-active,
.fade-enter-active {
  transition: opacity 0.2s cubic-bezier(0.69, 0.33, 0.16, 0.97);
}
.fade-enter-from,
.fade-leave-to {
  opacity: 0;
}

.season-grid {
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  gap: 0.5rem;
  margin-bottom: 0.25rem;
}

.season-option {
  display: inline-flex;
  align-items: center;
  gap: 0.45rem;
  padding: 0.5rem 0.6rem;
  border: 1px solid rgba(17, 24, 39, 0.18);
  border-radius: 6px;
  background: rgba(255, 255, 255, 0.7);
  cursor: pointer;
  user-select: none;
  transition: border-color 0.15s ease, box-shadow 0.15s ease, background-color 0.15s ease;
  position: relative;
}

.season-option input {
  margin: 0;
}

.season-option:hover {
  border-color: rgba(17, 24, 39, 0.28);
}

.season-option--selected {
  border-color: rgba(34, 197, 94, 1);
  background: rgba(34, 197, 94, 0.10);
  box-shadow:
    0 0 0 2px rgba(34, 197, 94, 0.24),
    0 4px 12px rgba(34, 197, 94, 0.14);
}

.season-option:focus-within {
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.25);
  border-color: rgba(59, 130, 246, 0.7);
}
</style>
