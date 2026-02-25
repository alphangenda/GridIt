<template>
  <AuthenticationLayout v-if="!userStore.user.email || isAuthenticationPath"/>
  <DashboardLayout v-else/>
</template>

<script lang="ts" setup>
import {computed, onMounted} from "vue";
import {useRouter} from "vue-router";
import {useUserStore} from "@/stores/userStore";
import AuthenticationLayout from "@/components/layouts/AuthenticationLayout.vue";
import DashboardLayout from "@/components/layouts/DashboardLayout.vue";
import {useUserService} from "@/inversify.config";

const router = useRouter();
const userStore = useUserStore();
const userService = useUserService();

const authenticationRoutes = ['login', 'twoFactor', 'forgotPassword', 'resetPassword']
let isAuthenticationPath = computed(() => {
  return authenticationRoutes.includes(router.currentRoute.value.name as string)
});

onMounted(async () => {
  // Skip on auth pages: user is not logged in, getCurrentUser() would 401 and can cause infinite loading
  if (isAuthenticationPath.value) return;
  if (!userStore.user.email) {
    try {
      const user = await userService.getCurrentUser();
      if (user?.email) userStore.setUser(user);
    } catch {
      // Not authenticated, leave store as-is
    }
  }
});

</script>

<style lang="scss">
@use "./sass/index.scss";
</style>

