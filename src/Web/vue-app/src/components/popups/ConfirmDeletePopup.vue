<template>
  <Transition name="fade">
    <div class="popup" role="dialog" aria-modal="true">
      <div class="popup__bg" @click="onClose"></div>
      <div class="popup__container">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("global.actions.delete") }}</p>
        </div>
        <div class="popup__content">
          <div class="popup__block confirm-delete">
            <p class="confirm-delete__text">{{ message }}</p>
            <div class="form__submit confirm-delete__actions">
              <button
                class="btn btn--fullscreen btn--red"
                type="button"
                :disabled="isLoading"
                @click="emit('confirm')"
              >
                {{ isLoading ? t("global.loading") : t("global.actions.delete") }}
              </button>
              <button
                class="btn btn--fullscreen"
                type="button"
                :disabled="isLoading"
                @click="onClose"
              >
                {{ t("global.cancel") }}
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

const props = defineProps<{
  message: string;
  isLoading?: boolean;
}>();

const emit = defineEmits<{
  (event: "close"): void;
  (event: "confirm"): void;
}>();

const { t } = useI18n();

function onClose() {
  if (props.isLoading) return;
  emit("close");
}
</script>

<style scoped lang="scss">
.confirm-delete {
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
