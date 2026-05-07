<template>
  <Transition name="fade">
    <div class="popup" role="dialog" aria-modal="true">
      <div class="popup__bg" @click="emit('close')"></div>
      <div class="popup__container">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("pages.examDetail.resetConfirmTitle") }}</p>
        </div>
        <div class="popup__content">
          <div class="popup__block confirm-reset">
            <p class="confirm-reset__text">
              {{ t("pages.examDetail.resetConfirmMessage") }}
            </p>
            <div class="form__submit confirm-reset__actions">
              <button class="btn btn--fullscreen" type="button" :disabled="isLoading" @click="emit('close')">
                {{ t("global.cancel") }}
              </button>
              <button
                class="btn btn--fullscreen btn--red"
                type="button"
                :disabled="isLoading"
                @click="emit('confirm')"
              >
                {{ isLoading ? t("pages.examDetail.resetting") : t("pages.examDetail.resetConfirmAction") }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </div>
  </Transition>
</template>

<script setup lang="ts">
import { useI18n } from "vue3-i18n";

defineProps<{
  isLoading?: boolean;
}>();

const emit = defineEmits<{
  (event: "close"): void;
  (event: "confirm"): void;
}>();

const { t } = useI18n();
</script>

<style scoped lang="scss">
.confirm-reset {
  &__text {
    margin-bottom: 24px;
    line-height: 1.5;
    text-align: center;
  }

  &__actions {
    display: flex;
    flex-direction: column;
    gap: 12px;
  }
}

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
