<template>
  <nav v-if="crumbs.length > 1" class="breadcrumb" aria-label="Fil d'Ariane">
    <ol class="breadcrumb__list">
      <li v-for="(crumb, i) in crumbs" :key="i" class="breadcrumb__item">
        <RouterLink v-if="crumb.to && i < crumbs.length - 1" :to="crumb.to" class="breadcrumb__link">
          {{ crumb.label }}
        </RouterLink>
        <span v-else class="breadcrumb__current">{{ crumb.label }}</span>
        <span v-if="i < crumbs.length - 1" class="breadcrumb__separator">›</span>
      </li>
    </ol>
  </nav>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useRoute, type RouteLocationRaw } from "vue-router";
import { useI18n } from "vue3-i18n";
import { useClassesStore } from "@/stores/classesStore";

interface Crumb {
  label: string;
  to?: RouteLocationRaw;
}

const route = useRoute();
const { t } = useI18n();
const classesStore = useClassesStore();

const groupNames = ref<Record<string, string>>({});
const examNames = ref<Record<string, string>>({});

function getClassName(classId: string): string {
  const cls = classesStore.getClasses.find((c) => c.id === classId);
  if (cls?.name) return cls.name;
  // Fallback: query param from grids page (other professor's course)
  const queryName = route.query.courseName as string | undefined;
  return queryName ?? classId;
}

function getExamNameFromQuery(): string | undefined {
  return route.query.examName as string | undefined;
}

function getProfessorFromQuery(): string | undefined {
  return route.query.professor as string | undefined;
}

function isReadOnly(): boolean {
  return route.query.readOnly === "1";
}

async function fetchGroupName(classId: string, groupId: string): Promise<string> {
  const key = `${classId}:${groupId}`;
  if (groupNames.value[key]) return groupNames.value[key];
  try {
    const res = await fetch(`/api/classes/${classId}/groups`);
    if (res.ok) {
      const groups = (await res.json()) as { id: string; name: string }[];
      const found = groups.find((g) => g.id === groupId);
      if (found) {
        groupNames.value[key] = found.name;
        return found.name;
      }
    }
  } catch { /* ignore */ }
  return groupId;
}

async function fetchExamName(classId: string, examId: string, groupId?: string): Promise<string> {
  if (examNames.value[examId]) return examNames.value[examId];
  const exams = classesStore.getExamsForClass(classId);
  const cached = exams.find((e) => e.id === examId);
  if (cached?.name) {
    examNames.value[examId] = cached.name;
    return cached.name;
  }
  try {
    const url = groupId
      ? `/api/classes/${classId}/groups/${groupId}/exams`
      : `/api/classes/${classId}/exams`;
    const res = await fetch(url);
    if (res.ok) {
      const data = (await res.json()) as { id: string; name: string }[];
      const found = data.find((e) => e.id === examId);
      if (found) {
        examNames.value[examId] = found.name;
        return found.name;
      }
    }
  } catch { /* ignore */ }
  return examId;
}

const asyncCrumbs = ref<Crumb[]>([]);

async function buildCrumbs() {
  const name = route.name as string;
  const params = route.params;
  const result: Crumb[] = [];

  if (name?.startsWith("classes")) {
    result.push({ label: t("navigation.classes"), to: { name: "classes.index" } });

    const classId = params.classId as string;
    const groupId = params.groupId as string;
    const examId = params.examId as string;

    if (classId) {
      const className = getClassName(classId);

      if (name === "classes.detail") {
        result.push({ label: className });
      } else if (name === "classes.groupExams" && groupId) {
        result.push({ label: className, to: { name: "classes.detail", params: { classId } } });
        const groupName = await fetchGroupName(classId, groupId);
        result.push({ label: groupName });
      } else if (name === "classes.examDetail" && groupId && examId) {
        result.push({ label: className, to: { name: "classes.detail", params: { classId } } });
        const groupName = await fetchGroupName(classId, groupId);
        result.push({ label: groupName, to: { name: "classes.groupExams", params: { classId, groupId } } });
        const examName = await fetchExamName(classId, examId, groupId);
        result.push({ label: examName });
      } else if (name === "classes.examDetailDirect" && examId) {
        if (isReadOnly()) {
          // Grille d'un autre professeur
          const professor = getProfessorFromQuery();
          if (professor) {
            result.splice(0, result.length); // Reset
            result.push({ label: t("routes.grids.name"), to: { name: "grids" } });
            result.push({ label: professor });
          }
          result.push({ label: className });
          const examName = getExamNameFromQuery() ?? await fetchExamName(classId, examId);
          result.push({ label: examName });
        } else {
          result.push({ label: className, to: { name: "classes.detail", params: { classId } } });
          const examName = getExamNameFromQuery() ?? await fetchExamName(classId, examId);
          result.push({ label: examName });
        }
      }
    }
  } else if (name === "grids") {
    result.push({ label: t("routes.grids.name") });
  } else if (name?.startsWith("groupes")) {
    result.push({ label: t("navigation.groups"), to: { name: "groupes" } });

    const groupId = params.groupId as string;
    if (name === "groupes.students" && groupId) {
      const groupName = groupNames.value[`:${groupId}`] ?? groupId;
      // Fetch from groups list
      try {
        const res = await fetch("/api/groups");
        if (res.ok) {
          const groups = (await res.json()) as { id: string; name: string }[];
          const found = groups.find((g) => g.id === groupId);
          if (found) {
            groupNames.value[`:${groupId}`] = found.name;
            result.push({ label: found.name });
          } else {
            result.push({ label: groupId });
          }
        }
      } catch {
        result.push({ label: groupId });
      }
    }
  } else if (name?.startsWith("sessions")) {
    result.push({ label: t("routes.sessions.name"), to: { name: "sessions.index" } });

    if (name === "sessions.detail") {
      result.push({ label: t("routes.sessions.name") });
    }
  } else if (name?.startsWith("admin")) {
    if (name.includes("members")) {
      result.push({ label: t("routes.admin.children.members.name"), to: { name: "admin.children.members.index" } });
      if (name === "admin.children.members.add") {
        result.push({ label: t("routes.admin.children.members.add.name") });
      } else if (name === "admin.children.members.edit") {
        result.push({ label: t("routes.admin.children.members.edit.name") });
      }
    } else if (name.includes("programs")) {
      result.push({ label: t("routes.admin.children.programs.name") });
    }
  } else if (name === "evaluation") {
    const classId = params.classId as string;
    const examId = params.examId as string;
    result.push({ label: t("navigation.classes"), to: { name: "classes.index" } });
    if (classId) {
      const className = getClassName(classId);
      result.push({ label: className, to: { name: "classes.detail", params: { classId } } });
    }
    if (examId && classId) {
      const examName = await fetchExamName(classId, examId);
      result.push({ label: examName });
    }
  } else if (name === "books" || name?.startsWith("books.")) {
    result.push({ label: t("routes.books.name"), to: { name: "books.index" } });
    if (name === "books.children.add") {
      result.push({ label: t("routes.books.children.add.name") });
    } else if (name === "books.children.edit") {
      result.push({ label: t("routes.books.children.edit.name") });
    }
  }

  asyncCrumbs.value = result;
}

watch(() => [route.name, route.params, classesStore.getClasses], () => buildCrumbs(), { immediate: true, deep: true });

const crumbs = computed(() => asyncCrumbs.value);
</script>

<style scoped lang="scss">
.breadcrumb {
  padding: 8px 0;
  margin-bottom: 4px;
}

.breadcrumb__list {
  list-style: none;
  display: flex;
  align-items: center;
  gap: 4px;
  margin: 0;
  padding: 0;
  flex-wrap: wrap;
}

.breadcrumb__item {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 0.875rem;
}

.breadcrumb__link {
  color: #6b7280;
  text-decoration: none;
  transition: color 0.15s ease;

  &:hover {
    color: #7a6a55;
  }
}

.breadcrumb__current {
  color: #1a1a2e;
  font-weight: 600;
}

.breadcrumb__separator {
  color: #d1d5db;
  font-size: 0.75rem;
}
</style>
