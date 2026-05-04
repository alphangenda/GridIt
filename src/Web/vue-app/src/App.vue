<template>
  <AuthenticationLayout v-if="isCheckingAuth || !userStore.user.email || isAuthenticationPath"/>
  <DashboardLayout v-else/>
</template>

<script lang="ts" setup>
import {computed, onMounted, ref} from "vue";
import {useRouter} from "vue-router";
import {useUserStore} from "@/stores/userStore";
import AuthenticationLayout from "@/components/layouts/AuthenticationLayout.vue";
import DashboardLayout from "@/components/layouts/DashboardLayout.vue";
import {useUserService} from "@/inversify.config";

const router = useRouter();
const userStore = useUserStore();
const userService = useUserService();

const authenticationRoutes = ['login', 'twoFactor', 'forgotPassword', 'resetPassword', 'confirmEmail', 'register']
let isAuthenticationPath = computed(() => {
  return authenticationRoutes.includes(router.currentRoute.value.name as string)
});

// Prevents showing DashboardLayout before auth is verified
const isCheckingAuth = ref(!isAuthenticationPath.value && !!userStore.user.email);

onMounted(async () => {
  // Skip on auth pages: user is not logged in, getCurrentUser() would 401 and can cause infinite loading
  if (isAuthenticationPath.value) return;

  try {
    const user = await userService.getCurrentUser();
    if (user?.email) {
      userStore.setUser(user);
    } else {
      userStore.reset();
      await router.push({ name: "login" });
    }
  } catch {
    // Not authenticated: clear stale persisted data and redirect to login
    userStore.reset();
    await router.push({ name: "login" });
  } finally {
    isCheckingAuth.value = false;
  }
});

</script>

<style lang="scss">
@use "./sass/index.scss";
</style>

