<template>
  <div class="popup-overlay" @click.self="$emit('close')">
    <div class="popup">
      <div class="popup__header">
        <h2>{{ t('navigation.addGroup') }}</h2>
        <button type="button" class="popup__close" @click="$emit('close')">✕</button>
      </div>

      <div class="popup__tabs">
        <button :class="['popup__tab', { active: mode === 'new' }]" @click="mode = 'new'">
          {{ t('navigation.createNewGroup') }}
        </button>
        <button :class="['popup__tab', { active: mode === 'existing' }]" @click="mode = 'existing'">
          {{ t('navigation.addExistingGroup') }}
        </button>
      </div>

      <div class="popup__body">
        <template v-if="mode === 'new'">
          <label class="popup__label">{{ t('navigation.groupName') }}</label>
          <input
            v-model="newGroupName"
            type="text"
            class="popup__input"
            :placeholder="t('navigation.groupName')"
          />
        </template>

        <template v-else>
          <label class="popup__label">{{ t('navigation.selectGroup') }}</label>
          <select v-model="selectedGroupId" class="popup__input">
            <option value="">— {{ t('navigation.selectGroup') }} —</option>
            <option v-for="g in allGroups" :key="g.id" :value="g.id">{{ g.name }}</option>
          </select>
        </template>
      </div>

      <div class="popup__footer">
        <button type="button" class="btn btn--secondary" @click="$emit('close')">
          {{ t('global.cancel') }}
        </button>
        <button type="button" class="btn" :disabled="!canSubmit" @click="onSubmit">
          {{ t('global.add') }}
        </button>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useRouter } from 'vue-router';
import { useI18n } from 'vue3-i18n';

const props = defineProps<{ classId: string }>();
const emit = defineEmits<{ (e: 'close'): void }>();
const router = useRouter();
const { t } = useI18n();

const mode = ref<'new' | 'existing'>('new');
const newGroupName = ref('');
const selectedGroupId = ref('');
const allGroups = ref<{ id: string; name: string }[]>([]);

const canSubmit = computed(() =>
  mode.value === 'new' ? newGroupName.value.trim().length > 0 : selectedGroupId.value !== ''
);

async function fetchAllGroups() {
  const res = await fetch('/api/groups');
  if (!res.ok) return;
  const data = await res.json();
  allGroups.value = data.map((g: any) => ({ id: String(g.id), name: String(g.name) }));
}

async function onSubmit() {
  const body =
    mode.value === 'new'
      ? { name: newGroupName.value.trim() }
      : { groupId: selectedGroupId.value };

  const res = await fetch(`/api/classes/${props.classId}/groups`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(body),
  });

  if (res.ok) {
    const created = await res.json();
    const groupId = created.groupId ?? created.id;
    emit('close');
    await router.push({
      name: 'classes.groupExams',
      params: { classId: props.classId, groupId },
    });
  }
}

onMounted(fetchAllGroups);
</script>

<style scoped>
.popup-overlay {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 1000;
}
.popup {
  background: white;
  border-radius: 12px;
  padding: 24px;
  min-width: 360px;
  max-width: 480px;
  width: 100%;
}
.popup__header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}
.popup__header h2 {
  margin: 0;
  font-size: 18px;
}
.popup__close {
  background: none;
  border: none;
  font-size: 18px;
  cursor: pointer;
  opacity: 0.5;
}
.popup__close:hover {
  opacity: 1;
}
.popup__tabs {
  display: flex;
  gap: 8px;
  margin-bottom: 20px;
  border-bottom: 1px solid #eee;
  padding-bottom: 12px;
}
.popup__tab {
  background: none;
  border: none;
  padding: 6px 12px;
  border-radius: 6px;
  cursor: pointer;
  opacity: 0.5;
  font-size: 14px;
}
.popup__tab.active {
  opacity: 1;
  font-weight: 700;
  background: #f3f4f6;
}
.popup__body {
  margin-bottom: 8px;
}
.popup__label {
  display: block;
  font-size: 13px;
  font-weight: 600;
  margin-bottom: 6px;
}
.popup__input {
  width: 100%;
  padding: 8px 12px;
  border: 1px solid #d1d5db;
  border-radius: 6px;
  font-size: 14px;
  box-sizing: border-box;
}
.popup__footer {
  display: flex;
  justify-content: flex-end;
  gap: 8px;
  margin-top: 16px;
}
</style>
