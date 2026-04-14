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
              <label class="form__label" for="class-program">
                {{ t("pages.classes.program") }}
              </label>
              <select id="class-program" v-model="selectedProgramId" class="form__input">
                <option v-for="program in programs" :key="program.id" :value="program.id">
                  {{ program.name }}
                </option>
              </select>
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
import { useProgramService } from "@/inversify.config";
import { notifyError } from "@/notify";


type ProgramItem = {
  id: string;
  name: string;
};

const emit = defineEmits<{
  (event: "close"): void;
}>();

const { t } = useI18n();
const classesStore = useClassesStore();
const sessionsStore = useSessionsStore();
const programService = useProgramService();

const name = ref("");
const inputRef = ref<HTMLInputElement | null>(null);


const programs = ref<ProgramItem[]>([]);
const selectedProgramId = ref<string>("");

onMounted(async () => {
  inputRef.value?.focus();
  await loadPrograms();
});

async function loadPrograms() {
  try {
    const data = await programService.getAllPrograms();
    programs.value = data.map((x) => ({ id: String(x.id), name: String(x.name) }));
  } catch (error) {
    console.error(error);
    notifyError(t("pages.classes.programsLoadError"));
  }
}

async function handleSubmit() {
  const trimmed = name.value.trim();

  if (!trimmed) return;

  try {
    const createdClass = await classesStore.addClass(
      trimmed,
      [],
      selectedProgramId.value || undefined
    );

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

</style>
