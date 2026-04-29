<template>
  <Transition name="fade">
    <form class="popup" novalidate @submit.prevent="onSubmit">
      <div class="popup__bg" @click="$emit('close')"></div>
      <div class="popup__container">
        <div class="popup__header">
          <p class="popup__title h2-like">{{ t('navigation.addExam') }}</p>
        </div>
        <div class="popup__content">
          <div class="popup__block">
            <div class="form__group">
              <label class="form__label" for="exam-name">{{ t('navigation.examName') }}</label>
              <input
                id="exam-name"
                ref="inputRef"
                v-model="examName"
                class="form__input"
                :class="{ 'form__input--error': errorMessage }"
                type="text"
                required
                @input="errorMessage = ''"
              />
              <div v-if="errorMessage" class="form__error">{{ errorMessage }}</div>
            </div>
            <div class="form__submit">
              <button class="btn btn--fullscreen" type="submit" :disabled="!examName.trim()">
                {{ t('global.add') }}
              </button>
              <button class="btn btn--fullscreen btn--red" type="button" @click="$emit('close')">
                {{ t('global.cancel') }}
              </button>
            </div>
          </div>
        </div>
      </div>
    </form>
  </Transition>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue3-i18n';

const props = defineProps<{ classId: string; groupId: string }>();
const emit = defineEmits<{ (e: 'close'): void }>();
const router = useRouter();
const { t } = useI18n();

const examName = ref('');
const errorMessage = ref('');
const inputRef = ref<HTMLInputElement | null>(null);

onMounted(() => inputRef.value?.focus());

async function onSubmit() {
  const trimmed = examName.value.trim();
  if (!trimmed) return;
  errorMessage.value = '';

  const res = await fetch(`/api/classes/${props.classId}/groups/${props.groupId}/exams`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ name: trimmed }),
  });

  if (res.status === 409) {
    errorMessage.value = t('navigation.examNameExists');
    return;
  }

  if (res.ok) {
    const created = await res.json();
    emit('close');
    await router.push({
      name: 'classes.examDetail',
      params: { classId: props.classId, groupId: props.groupId, examId: created.id },
    });
  }
}
</script>

<style scoped lang="scss">
.form__input--error {
  border-color: #e74c3c !important;
}

.form__error {
  margin-top: 6px;
  font-size: 0.85rem;
  color: #c0392b;
  background: #fff3f3;
  border: 1px solid #f5c2c2;
  border-radius: 6px;
  padding: 6px 10px;
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
