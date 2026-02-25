<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="handleSubmit">
      <div class="popup__bg" @click="emit('close')"></div>
      <div class="popup__container">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t("navigation.addClass") }}</p>
          <button class="btn btn--import" type="button" @click="triggerFileInput">
            {{ t("navigation.importStudents") }}
          </button>
          <input
            ref="fileInputRef"
            type="file"
            accept=".json"
            style="display: none"
            @change="handleFileSelected"
          />
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
            <div v-if="students.length > 0" class="students-accordion">
              <button class="students-accordion__toggle" type="button" @click="expanded = !expanded">
                <span>{{ t("navigation.studentsImported", { count: students.length }) }}</span>
                <span class="students-accordion__chevron" :class="{ 'students-accordion__chevron--open': expanded }">&#9660;</span>
              </button>
              <div v-if="expanded" class="students-accordion__body">
                <table class="students-table">
                  <thead>
                    <tr>
                      <th>{{ t("navigation.studentNumber") }}</th>
                      <th>{{ t("navigation.studentFirstName") }}</th>
                      <th>{{ t("navigation.studentLastName") }}</th>
                    </tr>
                  </thead>
                  <tbody>
                    <tr v-for="(s, i) in students" :key="i">
                      <td>{{ s.number }}</td>
                      <td>{{ s.firstName }}</td>
                      <td>{{ s.lastName }}</td>
                    </tr>
                  </tbody>
                </table>
              </div>
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
const fileInputRef = ref<HTMLInputElement | null>(null);
const students = ref<{ number: string; firstName: string; lastName: string }[]>([]);
const expanded = ref(false);

onMounted(() => {
  inputRef.value?.focus();
});

function triggerFileInput() {
  fileInputRef.value?.click();
}

function handleFileSelected(event: Event) {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0];
  if (!file) return;

  const reader = new FileReader();
  reader.onload = (e) => {
    try {
      const parsed = JSON.parse(e.target?.result as string);
      if (!Array.isArray(parsed)) throw new Error();
      for (const item of parsed) {
        if (
          typeof item.number !== "string" ||
          typeof item.firstName !== "string" ||
          typeof item.lastName !== "string"
        ) {
          throw new Error();
        }
      }
      students.value = parsed.map((item: { number: string; firstName: string; lastName: string }) => ({
        number: item.number,
        firstName: item.firstName,
        lastName: item.lastName,
      }));
      expanded.value = true;
    } catch {
      notifyError(t("navigation.importStudentsFormatError"));
      students.value = [];
      expanded.value = false;
    }
  };
  reader.readAsText(file);
  input.value = "";
}

async function handleSubmit() {
  const trimmed = name.value.trim();
  if (!trimmed) return;
  try {
    await classesStore.addClass(trimmed, students.value.length > 0 ? students.value : undefined);
    emit("close");
  } catch {
    notifyError(t("pages.classes.addError"));
  }
}
</script>

<style scoped lang="scss">
.popup__header {
  position: relative;
}

.btn--import {
  position: absolute;
  right: 12px;
  top: 50%;
  transform: translateY(-50%);
  font-size: 0.85rem;
  padding: 4px 10px;
  cursor: pointer;
}

.students-accordion {
  margin: 12px 0;

  &__toggle {
    display: flex;
    align-items: center;
    justify-content: space-between;
    width: 100%;
    background: none;
    border: 1px solid #e0e0e0;
    border-radius: 6px;
    padding: 8px 12px;
    cursor: pointer;
    font-size: 0.9rem;
    font-weight: 500;
    color: #4caf50;

    &:hover {
      background-color: #f5f5f5;
    }
  }

  &__chevron {
    font-size: 0.7rem;
    transition: transform 0.2s ease;

    &--open {
      transform: rotate(180deg);
    }
  }

  &__body {
    border: 1px solid #e0e0e0;
    border-top: none;
    border-radius: 0 0 6px 6px;
    max-height: 200px;
    overflow-y: auto;
  }
}

.students-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.85rem;

  th, td {
    padding: 6px 10px;
    text-align: left;
  }

  th {
    background-color: #f9f9f9;
    font-weight: 600;
    position: sticky;
    top: 0;
  }

  tbody tr:not(:last-child) {
    border-bottom: 1px solid #f0f0f0;
  }

  tbody tr:hover {
    background-color: #fafafa;
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
