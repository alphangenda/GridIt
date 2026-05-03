<template>
  <div class="otp-input">
    <input
      v-for="(_, i) in digits"
      :key="i"
      :ref="(el) => { if (el) inputs[i] = el as HTMLInputElement }"
      :value="digits[i]"
      type="text"
      inputmode="numeric"
      maxlength="1"
      autocomplete="one-time-code"
      class="otp-input__box"
      @input="onInput($event, i)"
      @keydown="onKeydown($event, i)"
      @focus="onFocus($event)"
      @paste.prevent="onPaste($event)"
    />
  </div>
</template>

<script lang="ts" setup>
import { ref, watch } from 'vue'

const props = defineProps<{ modelValue: string }>()
const emit = defineEmits<{ 'update:modelValue': [value: string] }>()

const digits = ref<string[]>(Array(6).fill(''))
const inputs = ref<HTMLInputElement[]>([])

watch(
  () => props.modelValue,
  (val) => {
    const clean = val.replace(/\D/g, '').slice(0, 6)
    for (let i = 0; i < 6; i++) {
      digits.value[i] = clean[i] ?? ''
    }
  },
  { immediate: true }
)

function onInput(event: Event, index: number) {
  const char = (event.target as HTMLInputElement).value.replace(/\D/g, '').slice(-1)
  digits.value[index] = char
  emit('update:modelValue', digits.value.join(''))
  if (char && index < 5) inputs.value[index + 1]?.focus()
}

function onKeydown(event: KeyboardEvent, index: number) {
  if (event.key === 'Backspace' && !digits.value[index] && index > 0) {
    inputs.value[index - 1]?.focus()
  }
}

function onFocus(event: FocusEvent) {
  (event.target as HTMLInputElement).select()
}

function onPaste(event: ClipboardEvent) {
  const pasted = (event.clipboardData?.getData('text') ?? '').replace(/\D/g, '').slice(0, 6)
  if (pasted.length === 6) {
    digits.value = pasted.split('')
    emit('update:modelValue', pasted)
    inputs.value[5]?.focus()
  }
}
</script>

<style lang="scss" scoped>
@use "@/sass/tools" as *;

.otp-input {
  display: flex;
  gap: 8px;
  justify-content: center;
  margin-bottom: 24px;

  &__box {
    width: 48px;
    height: 48px;
    text-align: center;
    font-size: rem(20);
    font-weight: 600;
    border: 1px solid $color-border;
    border-radius: $common-border-radius;
    padding: 0;
    background-color: $color-white;
    transition: border-color 0.2s cb(snappy), box-shadow 0.2s cb(snappy);

    &:focus {
      outline: none;
      border-color: $color-green;
      box-shadow: 0 0 0 3px rgba($color-green, 0.1);
    }
  }
}
</style>
