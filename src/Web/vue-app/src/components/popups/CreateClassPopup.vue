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
import { notifyError } from "@/notify";

const emit = defineEmits<{
  (event: "close"): void;
}>();

const { t } = useI18n();
const classesStore = useClassesStore();

const name = ref("");
const inputRef = ref<HTMLInputElement | null>(null);

onMounted(() => {
  inputRef.value?.focus();
});

async function handleSubmit() {
  const trimmed = name.value.trim();
  if (!trimmed) return;
  try {
    await classesStore.addClass(trimmed);
    emit("close");
  } catch {
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
