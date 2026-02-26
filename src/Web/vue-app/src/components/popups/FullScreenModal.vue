<template>
  <teleport to="body">
    <div
      v-if="modelValue"
      class="fs-overlay"
      role="dialog"
      aria-modal="true"
      @keydown.esc.prevent="close"
      tabindex="-1"
    >
      <div class="fs-backdrop" @click="close"></div>

      <div
        class="fs-panel"
        :class="{ 'fs-panel--expanded': expanded }"
        >
        <button class="fs-close" type="button" @click="close" aria-label="Fermer">
          ✕
        </button>

        <div class="fs-content">
          <slot/>
        </div>
      </div>
    </div>
  </teleport>
</template>

<script setup lang="ts">
import { watch, nextTick, useAttrs, computed } from "vue";

const attrs = useAttrs();

const expanded = computed(() =>
  typeof attrs.class === "string" &&
  attrs.class.includes("modal--expanded")
);

const props = defineProps<{
  modelValue: boolean;
  closeOnBackdrop?: boolean;
}>();

const emit = defineEmits<{
  (e: "update:modelValue", value: boolean): void;
}>();

function close() {
  emit("update:modelValue", false);
}

watch(
  () => props.modelValue,
  async (open) => {
    if (open) {
      await nextTick();
      const el = document.querySelector(".fs-overlay") as HTMLElement | null;
      el?.focus();
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }
  }
);
</script>

<style scoped>
.fs-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: grid;
  place-items: center;
  outline: none;
}


.fs-backdrop {
  position: absolute;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
}

.fs-panel {
  position: relative;
  width: 1100px;
  max-width: 96vw;
  height: min(92vh, 900px);
  background: #fff;
  border-radius: 18px;
  overflow: hidden;
  box-shadow: 0 20px 80px rgba(0, 0, 0, 0.35);
  transition: width 0.25s ease;
}

.fs-panel--expanded {
  width: 1400px;
  max-width: 98vw;
}

.fs-close {
  position: absolute;
  top: 14px;
  right: 14px;
  width: 42px;
  height: 42px;
  border: 0;
  border-radius: 999px;
  cursor: pointer;
  background: rgba(0, 0, 0, 0.08);
  font-size: 20px;
  z-index: 10;
  transition: background 0.2s;
}

.fs-close:hover {
  background: rgba(0, 0, 0, 0.15);
}

.fs-content {
  height: 100%;
  padding: 24px;
  overflow: auto;
}
</style>
