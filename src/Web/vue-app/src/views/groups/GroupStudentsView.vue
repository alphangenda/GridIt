<template>
  <div class="content-grid">
    <div class="content-grid__header">
      <div class="content-grid__back">
        <router-link :to="{ name: 'groupes' }" class="content-grid__back-link" aria-label="Retour">
          &lt;
        </router-link>
        <h1>{{ groupName || t('navigation.groups') }}</h1>
      </div>
    </div>

    <Card>
      <div class="add-student">
        <h2 class="add-student__title">{{ t('navigation.addStudent') }}</h2>
        <form class="add-student__form" @submit.prevent="onAddStudent">
          <input
            v-model="newNumber"
            class="form__input"
            type="text"
            :placeholder="t('navigation.studentNumber')"
            required
          />
          <input
            v-model="newFirstName"
            class="form__input"
            type="text"
            :placeholder="t('navigation.studentFirstName')"
            required
          />
          <input
            v-model="newLastName"
            class="form__input"
            type="text"
            :placeholder="t('navigation.studentLastName')"
            required
          />
          <button
            class="btn"
            type="submit"
            :disabled="isAdding || !newNumber.trim() || !newFirstName.trim() || !newLastName.trim()"
          >
            {{ isAdding ? t('global.loading') : t('global.add') }}
          </button>
        </form>
      </div>
    </Card>

    <Card>
      <div v-if="loading" class="students-empty">{{ t('global.loading') }}</div>
      <div v-else-if="students.length === 0" class="students-empty">
        {{ t('navigation.noStudents') }}
      </div>
      <ul v-else class="students-list">
        <li v-for="student in students" :key="student.id" class="students-list__item">
          <span class="students-list__number">{{ student.number }}</span>
          <span class="students-list__name">{{ student.lastName }}, {{ student.firstName }}</span>
          <button
            type="button"
            class="students-list__delete"
            :aria-label="t('global.actions.delete')"
            :title="t('global.actions.delete')"
            @click="onDeleteStudent(student)"
          >
            <IconDelete class="icon icon--black" />
          </button>
        </li>
      </ul>
    </Card>

    <ConfirmDeletePopup
      v-if="pendingDelete"
      :message="t('navigation.deleteStudentConfirm')"
      :is-loading="isDeleting"
      @close="pendingDelete = null"
      @confirm="confirmDelete"
    />
  </div>
</template>

<script setup lang="ts">
import { ref, onMounted } from 'vue';
import { useRoute } from 'vue-router';
import { useI18n } from 'vue3-i18n';
import Card from '@/components/layouts/items/Card.vue';
import ConfirmDeletePopup from '@/components/popups/ConfirmDeletePopup.vue';
import { notifySuccess, notifyError } from '@/notify';
import IconDelete from '@/assets/icons/icon__delete.svg';

const { t } = useI18n();
const route = useRoute();

const groupId = route.params.groupId as string;
const groupName = ref('');
const students = ref<{ id: string; number: string; firstName: string; lastName: string }[]>([]);
const loading = ref(true);

const newNumber = ref('');
const newFirstName = ref('');
const newLastName = ref('');
const isAdding = ref(false);

const pendingDelete = ref<{ id: string; firstName: string; lastName: string } | null>(null);
const isDeleting = ref(false);

async function fetchGroupName() {
  const res = await fetch('/api/groups');
  if (!res.ok) return;
  const data = await res.json();
  const group = data.find((g: any) => String(g.id) === groupId);
  if (group) groupName.value = group.name;
}

async function fetchStudents() {
  loading.value = true;
  try {
    const res = await fetch(`/api/groups/${groupId}/students`);
    if (!res.ok) return;
    const data = await res.json();
    students.value = data.map((s: any) => ({
      id: String(s.id),
      number: String(s.number),
      firstName: String(s.firstName),
      lastName: String(s.lastName),
    }));
  } finally {
    loading.value = false;
  }
}

async function onAddStudent() {
  if (!newNumber.value.trim() || !newFirstName.value.trim() || !newLastName.value.trim()) return;
  isAdding.value = true;
  try {
    const res = await fetch(`/api/groups/${groupId}/students`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        number: newNumber.value.trim(),
        firstName: newFirstName.value.trim(),
        lastName: newLastName.value.trim(),
      }),
    });
    if (!res.ok) throw new Error();
    newNumber.value = '';
    newFirstName.value = '';
    newLastName.value = '';
    notifySuccess(t('navigation.studentAdded'));
    await fetchStudents();
  } catch {
    notifyError(t('navigation.studentAddError'));
  } finally {
    isAdding.value = false;
  }
}

function onDeleteStudent(student: { id: string; firstName: string; lastName: string }) {
  pendingDelete.value = student;
}

async function confirmDelete() {
  if (!pendingDelete.value) return;
  isDeleting.value = true;
  try {
    const res = await fetch(`/api/groups/${groupId}/students/${pendingDelete.value.id}`, {
      method: 'DELETE',
    });
    if (!res.ok) throw new Error();
    pendingDelete.value = null;
    notifySuccess(t('navigation.studentDeleted'));
    await fetchStudents();
  } catch {
    notifyError(t('navigation.studentDeleteError'));
  } finally {
    isDeleting.value = false;
  }
}

onMounted(async () => {
  await Promise.all([fetchGroupName(), fetchStudents()]);
});
</script>

<style scoped>
.add-student {
  padding: 20px;
  display: grid;
  gap: 12px;
}
.add-student__title {
  font-size: 15px;
  font-weight: 700;
  margin: 0;
}
.add-student__form {
  display: flex;
  gap: 10px;
  flex-wrap: wrap;
  align-items: flex-end;
}
.add-student__form .form__input {
  flex: 1;
  min-width: 120px;
}
.students-empty {
  padding: 32px;
  text-align: center;
  opacity: 0.6;
  font-weight: 600;
}
.students-list {
  list-style: none;
  margin: 0;
  padding: 0;
}
.students-list__item {
  display: flex;
  align-items: center;
  gap: 16px;
  padding: 10px 20px;
  border-bottom: 1px solid rgba(0, 0, 0, 0.08);
}
.students-list__item:last-child {
  border-bottom: none;
}
.students-list__number {
  font-size: 13px;
  opacity: 0.55;
  min-width: 80px;
}
.students-list__name {
  font-weight: 600;
  font-size: 15px;
  flex: 1;
}
.students-list__delete {
  background: transparent;
  border: none;
  cursor: pointer;
  padding: 4px;
  border-radius: 4px;
  display: inline-flex;
  align-items: center;
  justify-content: center;
  opacity: 0.5;
  transition: opacity 0.15s, background-color 0.15s;
}
.students-list__delete:hover {
  opacity: 1;
  background: #fee2e2;
}
</style>
