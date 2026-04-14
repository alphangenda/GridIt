<template>
  <EasyDataTable
      :empty-message="t('global.table.noData')"
      :filter-options="filterOptions"
      :headers="headers"
      :hide-footer="isSoloItem"
      :hide-rows-per-page="true"
      :items="items"
      :loading="isLoading"
      :rows-of-page-separator-message="t('global.table.of')"
      :rows-per-page="isSoloItem ? 1 : 10"
      :search-value="searchValue"
      :table-min-height="0"
      alternating
      buttons-pagination
      header-item-class-name="vue3-easy-data-table__header-item"
      theme-color="#528965"
      @click-row="onClickRow"
  >
    <template #item-status="item">
      <slot name="item-status" v-bind="item">
        <div class="tag">
          <p>{{ item.status }}</p>
        </div>
      </slot>
    </template>
    <template #item-actions="item">
      <p v-if="item && item.actions" class="vue3-easy-data-table__actions">
        <router-link
            v-if="item.actions.edit"
            v-tippy="t(`global.actions.update`)"
            :to="item.actions.edit"
            class="vue3-easy-data-table__action"
            @click.stop
        >
          <IconEdit class="icon icon--black"/>
        </router-link>
        <button
            v-if="item.actions.delete && item.id"
            v-tippy="t(`global.actions.delete`)"
            class="vue3-easy-data-table__action red-bg"
            type="button"
            @click.stop="handleDelete(item)"
        >
          <IconDelete class="icon icon--black"/>
        </button>
      </p>
    </template>

  </EasyDataTable>
</template>

<script lang="ts" setup>
import type {FilterOption, Header, Item} from "vue3-easy-data-table"
import {useI18n} from "vue3-i18n"
import {useRouter} from "vue-router"
import IconEdit from "@/assets/icons/icon__edit.svg"
import IconDelete from "@/assets/icons/icon__delete.svg"

const {t} = useI18n()

// eslint-disable-next-line
defineProps<{
  headers: Header[],
  items: Item[],
  filterOptions?: FilterOption[],
  isLoading?: boolean,
  searchValue?: string
  isSoloItem?: boolean
}>()

// eslint-disable-next-line
const emit = defineEmits<{
  (event: "delete", item: any): void
  (event: "row-click", item: any): void
}>()

const router = useRouter()

function onClickRow(item: any) {
  if (item?.actions?.view) {
    router.push(item.actions.view)
    return
  }

  emit("row-click", item)
}

function handleDelete(item: any) {
  emit("delete", item)
}
</script>
