import { defineStore } from "pinia";
import { useSessionService } from "@/inversify.config";
import type { SessionItem } from "@/types/entities";

interface SessionsState {
  sessions: SessionItem[];
}

export const useSessionsStore = defineStore("sessions", {
  state: (): SessionsState => ({
    sessions: [],
  }),

  getters: {
    getSessions: (state) => state.sessions,
  },

  actions: {
    async fetchSessions() {
      const sessionService = useSessionService();
      this.sessions = await sessionService.getAllSessions();
    },
    async addSession(name: string, classIds?: string[]) {
      const sessionService = useSessionService();
      const created = await sessionService.createSession(name, classIds);
      this.sessions.push(created);
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
    },
  },
});
