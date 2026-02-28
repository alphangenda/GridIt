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
              <label class="form__label" for="session-name">{{ t("navigation.sessionName") }}</label>
              <input
                id="session-name"
                ref="inputRef"
                v-model="name"
                class="form__input"
                type="text"
                required
              />
            </div>
            <div class="form__group">
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
import { ref, onMounted, computed } from "vue";
import { useI18n } from "vue3-i18n";
import { useSessionsStore } from "@/stores/sessionsStore";
import { useClassesStore } from "@/stores/classesStore";
import { notifyError } from "@/notify";

const emit = defineEmits<{
  (event: "close"): void;
}>();

const { t } = useI18n();
const sessionsStore = useSessionsStore();
const classesStore = useClassesStore();

const name = ref("");
const selectedClassIds = ref<string[]>([]);
const inputRef = ref<HTMLInputElement | null>(null);
const classes = computed(() => classesStore.getClasses);

onMounted(() => {
  inputRef.value?.focus();
  classesStore.fetchClasses();
});

async function handleSubmit() {
  const trimmed = name.value.trim();
  if (!trimmed) return;
  try {
    await sessionsStore.addSession(trimmed, selectedClassIds.value);
    emit("close");
    // Reset form
    name.value = "";
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
</style>
