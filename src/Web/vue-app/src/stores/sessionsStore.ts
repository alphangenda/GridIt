import { defineStore } from "pinia";
import { useSessionService } from "@/inversify.config";
import type { SessionItem } from "@/types/entities";

interface SessionsState {
  sessions: SessionItem[];
  selectedSessionId: string | null;
}

export const useSessionsStore = defineStore("sessions", {
  state: (): SessionsState => ({
    sessions: [],
    selectedSessionId: null,
  }),

  getters: {
    getSessions: (state) => state.sessions,
    getSelectedSessionId: (state) => state.selectedSessionId,
    getSelectedSession: (state) =>
      state.selectedSessionId
        ? state.sessions.find((s) => s.id === state.selectedSessionId)
        : undefined,
  },

  actions: {
    async fetchSessions() {
      const sessionService = useSessionService();
      const sessions = await sessionService.getAllSessions();
      this.sessions = sessions;

      if (this.selectedSessionId) {
        const stillExists = this.sessions.some(
          (s) => s.id === this.selectedSessionId
        );
        if (!stillExists) {
          this.selectedSessionId = null;
        }
      }
    },
    async addSession(name: string, classIds?: string[]) {
      const sessionService = useSessionService();
      const created = await sessionService.createSession(name, classIds);
      this.sessions.push(created);
    },
    selectSession(sessionId: string | null) {
      this.selectedSessionId = sessionId;
    },
    async updateSession(sessionId: string, name: string, classIds?: string[]) {
      const sessionService = useSessionService();
      const updated = await sessionService.updateSession(sessionId, name, classIds);
      const index = this.sessions.findIndex((s) => s.id === sessionId);
      if (index !== -1) {
        this.sessions[index] = updated;
      }
    },
    async deleteSession(sessionId: string) {
      const sessionService = useSessionService();
      await sessionService.deleteSession(sessionId);
      this.sessions = this.sessions.filter((s) => s.id !== sessionId);
      if (this.selectedSessionId === sessionId) {
        this.selectedSessionId = null;
      }
    },
  },
});
