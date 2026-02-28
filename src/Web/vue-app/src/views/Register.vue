<template>
    <Card :title="t('routes.register.name')"
          class="form"
          :is-authentication="true"
          @keyup.enter="sendRegisterRequest">
        <Loader v-if="preventMultipleSubmit" />
        <FormInput :ref="addFormInputRef"
                   v-model="registerRequest.email"
                   :label="t('global.username')"
                   :rules="[required]"
                   name="email"
                   type="email"
                   @validated="handleValidation" />
        <FormInput :ref="addFormInputRef"
                   v-model="registerRequest.password"
                   :label="t('global.password')"
                   :rules="[required]"
                   name="password"
                   type="password"
                   @validated="handleValidation" />
        <button class="btn btn--full btn--purple btn--big" @click="sendRegisterRequest" :disabled="preventMultipleSubmit">
            {{ t('pages.register.submit') }}
        </button>
        <TextLink :path="{path: t('routes.login.path') }"
                  :text="t('pages.register.loginLink')" />
    </Card>
</template>

<script lang="ts" setup>
    import { ref } from "vue"
    import { useI18n } from "vue3-i18n"
    import { required } from "@/validation/rules"
    import { useRouter } from "vue-router";
    import { useAuthenticationService } from "@/inversify.config";
    import { notifyError, notifySuccess } from "@/notify";
    import { Status } from "@/validation";
    import { IRegisterRequest } from "@/types/requests/IRegisterRequest";
    import Card from "@/components/layouts/items/Card.vue";
    import FormInput from "@/components/forms/FormInput.vue";
    import Loader from "@/components/layouts/items/Loader.vue";
    import TextLink from "@/components/layouts/items/TextLink.vue";

    const { t } = useI18n()
    const router = useRouter();
    const authenticationService = useAuthenticationService()

    const registerRequest = ref<IRegisterRequest>({
        email: '',
        password: '',
        confirmEmailRelativeUrl: t('routes.confirmEmail.path')
    })

    const formInputs = ref<(typeof FormInput)[]>([])
    const inputValidationStatuses: any = {}
    const preventMultipleSubmit = ref<boolean>(false);

    function addFormInputRef(ref: typeof FormInput) {
        if (!formInputs.value.includes(ref))
            formInputs.value.push(ref)
    }

    async function handleValidation(name: string, validationStatus: Status) {
        inputValidationStatuses[name] = validationStatus.valid
    }

    async function sendRegisterRequest() {
        if (preventMultipleSubmit.value) return;
        preventMultipleSubmit.value = true;

        formInputs.value.forEach((x: typeof FormInput) => x.validateInput())
        if (Object.values(inputValidationStatuses).some(x => x === false)) {
            notifyError(t('validation.errorsInForm'))
            preventMultipleSubmit.value = false;
            return
        }

        let succeededOrNotResponse = await authenticationService.register(registerRequest.value)
        if (succeededOrNotResponse.succeeded) {
            notifySuccess(t('pages.register.success'))
            await router.push(t("routes.login.path"))
            preventMultipleSubmit.value = false;
            return;
        }

        let errorMessages = succeededOrNotResponse.getErrorMessages('pages.register.validation');
        if (errorMessages.length == 0)
            notifyError(t('pages.register.validation.errorOccured'))
        else
            notifyError(errorMessages[0])

        preventMultipleSubmit.value = false;
    }
</script>