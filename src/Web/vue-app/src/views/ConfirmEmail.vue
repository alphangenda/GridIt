<template>
    <Card :title="t('routes.confirmEmail.name')"
          class="form"
          :is-authentication="true">
        <Loader v-if="isLoading" />
        <div v-if="!isLoading && confirmed">
            <p>{{ t('pages.confirmEmail.success') }}</p>
            <TextLink :path="{path: t('routes.login.path') }"
                      :text="t('pages.confirmEmail.loginLink')" />
        </div>
        <p v-if="!isLoading && !confirmed">{{ t('pages.confirmEmail.error') }}</p>
    </Card>
</template>

<script lang="ts" setup>
    import { ref, onMounted } from "vue"
    import { useI18n } from "vue3-i18n"
    import { useRoute, useRouter } from "vue-router";
    import { useAuthenticationService } from "@/inversify.config";
    import Card from "@/components/layouts/items/Card.vue";
    import Loader from "@/components/layouts/items/Loader.vue";

    const { t } = useI18n()
    const route = useRoute();
    const router = useRouter();
    const authenticationService = useAuthenticationService()

    const isLoading = ref<boolean>(true);
    const confirmed = ref<boolean>(false);

    onMounted(async () => {
        const userId = route.query.userId as string;
        const token = route.query.token as string;

        if (!userId || !token) {
            isLoading.value = false;
            return;
        }

        const response = await authenticationService.confirmEmail({ userId, token })
        confirmed.value = response.succeeded;
        isLoading.value = false;
    })
</script>