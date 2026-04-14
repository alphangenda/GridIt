<template>
  <div class="user-avatar">
    <div class="user-avatar__img-container">
      <IconFaceMan class="icon icon--green"/>
    </div>
    <p class="user-avatar__name">{{ displayName }}</p>
  </div>
</template>

<script lang="ts" setup>
import { computed } from "vue";
import IconFaceMan from 'vue-material-design-icons/FaceMan.vue';
import {usePersonStore} from "@/stores/personStore";
import {useUserStore} from "@/stores/userStore";

const personStore = usePersonStore();
const userStore = useUserStore();

const displayName = computed(() => {
  if (personStore.person.fullName) return personStore.person.fullName;
  if (personStore.person.firstName) return personStore.person.firstName.substring(0, 5);
  const email = userStore.user.email || userStore.username;
  return email ? email.substring(0, 5) : "";
});
</script>