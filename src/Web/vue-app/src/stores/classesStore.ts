import { defineStore } from "pinia";
import { useClassService } from "@/inversify.config";
import type { ClassItem, ExamItem } from "@/types/entities";

interface ClassesState {
  classes: ClassItem[];
  examsByClassId: Record<string, ExamItem[]>;
}

export const useClassesStore = defineStore("classes", {
  state: (): ClassesState => ({
    classes: [],
    examsByClassId: {},
  }),

  getters: {
    getClasses: (state) => state.classes,
    getExamsForClass: (state) => (classId: string) =>
      state.examsByClassId[classId] ?? [],
  },

  actions: {
    async fetchClasses() {
      const classService = useClassService();
      this.classes = await classService.getAllClasses();
    },
    async fetchExams(classId: string) {
      const classService = useClassService();
      const exams = await classService.getExamsByClass(classId);
      this.examsByClassId[classId] = exams;
    },
    async addClass(name: string, students?: { number: string; firstName: string; lastName: string }[]) {
      const classService = useClassService();
      const created = await classService.createClass(name, students);
      this.classes.push(created);
    },
    async deleteClass(classId: string) {
      const classService = useClassService();
      await classService.deleteClass(classId);
      this.classes = this.classes.filter((c) => c.id !== classId);
      delete this.examsByClassId[classId];
    },
    async addExam(classId: string, name: string) {
      const classService = useClassService();
      const created = await classService.createExam(classId, name);
      if (!this.examsByClassId[classId]) this.examsByClassId[classId] = [];
      this.examsByClassId[classId].push(created);
    },
    async deleteExam(classId: string, examId: string) {
      const classService = useClassService();
      await classService.deleteExam(examId);
      if (this.examsByClassId[classId]) {
        this.examsByClassId[classId] = this.examsByClassId[classId].filter(
          (e) => e.id !== examId
        );
      }
    },
  },
});
