import { defineStore } from "pinia";

export const useThemeStore = defineStore("theme", {
  state: () => ({
    isDark: false,
  }),

  actions: {
    toggle() {
      this.isDark = !this.isDark;
      this.applyTheme();
    },
    applyTheme() {
      document.documentElement.setAttribute(
        "data-theme",
        this.isDark ? "dark" : "light"
      );
    },
  },

  persist: true,
});
