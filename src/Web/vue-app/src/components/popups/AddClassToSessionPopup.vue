<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="handleSubmit">
      <div class="popup__bg" @click="emit('close')"></div>
      <div class="popup__container">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("navigation.addClassToSession") }}</p>
        </div>
        <div class="popup__content">
          <div class="popup__block">
            <div class="form__group">
              <label class="form__label" for="session-classes">{{ t("navigation.classes") }}</label>
              <select
                id="session-classes"
                v-model="selectedClassIds"
                class="form__input"
                multiple
                size="8"
              >
                <option
                  v-for="classItem in availableClasses"
                  :key="classItem.id"
                  :value="classItem.id"
                  :disabled="isClassInSession(classItem.id)"
                >
                  {{ classItem.name }}
                </option>
              </select>
              <small class="form__help">{{ t("navigation.selectMultipleClasses") }}</small>
            </div>
            <div class="form__submit">
              <button class="btn btn--fullscreen" type="submit" :disabled="selectedClassIds.length === 0">
                {{ t("global.add") }}
              </button>
              <button class="btn btn--fullscreen btn--red" type="button" @click="emit('close')">
                {{ t("global.cancel") }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </form>
  </Transition>
</template>

<script lang="ts" setup>
import { ref, onMounted, computed } from "vue";
import { useI18n } from "vue3-i18n";
import { useSessionsStore } from "@/stores/sessionsStore";
import { useClassesStore } from "@/stores/classesStore";
import { notifyError, notifySuccess } from "@/notify";

const emit = defineEmits<{
  (event: "close"): void;
}>();

const props = defineProps<{
  sessionId: string;
  currentClassIds?: string[];
}>();

const { t } = useI18n();
const sessionsStore = useSessionsStore();
const classesStore = useClassesStore();

const selectedClassIds = ref<string[]>([]);

const availableClasses = computed(() => classesStore.getClasses);

const isClassInSession = (classId: string) => {
  return props.currentClassIds?.includes(classId) ?? false;
};

onMounted(() => {
  classesStore.fetchClasses();
});

async function handleSubmit() {
  if (selectedClassIds.value.length === 0) return;
  
  try {
    const session = sessionsStore.getSessions.find((s) => s.id === props.sessionId);
    if (!session) {
      notifyError(t("pages.sessions.sessionNotFound"));
      return;
    }

    // Get current class IDs and add new ones
    const currentIds = session.classIds || [];
    const newClassIds = [...new Set([...currentIds, ...selectedClassIds.value])];
    
    await sessionsStore.updateSession(props.sessionId, session.name || "", newClassIds);
    await sessionsStore.fetchSessions(); // Reload sessions to update the view
    notifySuccess(t("pages.sessions.classesAdded"));
    emit("close");
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

option:disabled {
  opacity: 0.5;
  font-style: italic;
}
</style>
