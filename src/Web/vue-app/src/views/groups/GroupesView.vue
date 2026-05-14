<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <h1>{{ t('navigation.groups') }}</h1>
      <div class="content-grid__actions">
        <button type="button" class="btn" @click="showCreatePopup = true">
          {{ t('navigation.createGroup') }}
        </button>
      </div>
    </div>
    <Card>
      <div v-if="loading" class="groups-empty">{{ t('global.loading') }}</div>
      <div v-else-if="groups.length === 0" class="groups-empty">
        {{ t('navigation.noGroups') }}
      </div>
      <DataTable
        v-else
        :headers="headers"
        :items="tableItems"
        @delete="onDeleteGroup"
      />
    </Card>

    <ImportGroupPopup
      v-if="showCreatePopup"
      class-id=""
      @close="onPopupClose"
    />

    <ConfirmDeletePopup
      v-if="pendingDelete"
      :message="t('navigation.deleteGroupConfirm')"
      :is-loading="isDeleting"
      @close="pendingDelete = null"
      @confirm="confirmDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from 'vue';
import { useI18n } from 'vue3-i18n';
import Card from '@/components/layouts/items/Card.vue';
import DataTable from '@/components/layouts/items/DataTable.vue';
import ImportGroupPopup from '@/components/popups/ImportGroupPopup.vue';
import ConfirmDeletePopup from '@/components/popups/ConfirmDeletePopup.vue';
import { notifySuccess, notifyError } from '@/notify';
import type { Header } from 'vue3-easy-data-table';

const { t } = useI18n();
const groups = ref<{ id: string; name: string; classes: { id: string; name: string }[] }[]>([]);
const loading = ref(true);
const showCreatePopup = ref(false);
const pendingDelete = ref<{ id: string } | null>(null);
const isDeleting = ref(false);

const headers: Header[] = [
  { text: t('navigation.groupName'), value: 'name', sortable: true },
  { text: t('navigation.classes'), value: 'classNames' },
  { text: t('global.table.actions'), value: 'actions', width: 120 },
];

const tableItems = computed(() =>
  groups.value.map((g) => ({
    id: g.id,
    name: g.name,
    classNames: g.classes.map((c) => c.name).join(', ') || '—',
    actions: {
      view: { name: 'groupes.students', params: { groupId: g.id } },
      delete: true,
    },
  }))
);

async function fetchGroups() {
  loading.value = true;
  try {
    const res = await fetch('/api/groups');
    if (!res.ok) return;
    const data = await res.json();
    groups.value = data.map((g: any) => ({
      id: String(g.id),
      name: String(g.name),
      classes: (g.classes ?? []).map((c: any) => ({ id: String(c.id), name: String(c.name) })),
    }));
  } finally {
    loading.value = false;
  }
}

async function onPopupClose() {
  showCreatePopup.value = false;
  await fetchGroups();
}

function onDeleteGroup(item: { id: string }) {
  pendingDelete.value = item;
}

async function confirmDelete() {
  if (!pendingDelete.value) return;
  isDeleting.value = true;
  try {
    const res = await fetch(`/api/groups/${pendingDelete.value.id}`, { method: 'DELETE' });
    if (!res.ok) throw new Error();
    pendingDelete.value = null;
    notifySuccess(t('navigation.groupDeleted'));
    await fetchGroups();
  } catch {
    notifyError(t('navigation.deleteError'));
  } finally {
    isDeleting.value = false;
  }
}

onMounted(fetchGroups);
</script>

<style scoped>
.groups-empty {
  padding: 32px;
  text-align: center;
  opacity: 0.6;
  font-weight: 600;
}
</style>

<style>
[data-theme="dark"] .groups-empty {
  color: rgba(255, 255, 255, 0.35);
}
</style>
