import { apiFetch } from "@/lib/api-client";

export type CustomFieldEntityType = "Party" | "PartyRelationship";

export type CustomFieldType =
  | "Text"
  | "Number"
  | "Currency"
  | "Date"
  | "Checkbox"
  | "Select"
  | "MultiSelect"
  | "EmailAddress"
  | "PhoneNumber";

export type CustomFieldOption = { value: string; label: string; color: string | null };

export type CustomFieldDefinitionDto = {
  id: string;
  entityType: CustomFieldEntityType;
  title: string;
  apiSlug: string;
  fieldType: CustomFieldType;
  description: string | null;
  isRequired: boolean;
  isUnique: boolean;
  isDefaultValueEnabled: boolean;
  defaultValue: string | null;
  isMultiselect: boolean;
  options: CustomFieldOption[];
  activo: boolean;
};

export type CreateCustomFieldDefinitionInput = {
  entityType: CustomFieldEntityType;
  title: string;
  apiSlug: string | null;
  fieldType: CustomFieldType;
  description: string | null;
  isRequired: boolean;
  isUnique: boolean;
  isDefaultValueEnabled: boolean;
  defaultValue: string | null;
  isMultiselect: boolean;
  options: CustomFieldOption[] | null;
};

export type UpdateCustomFieldDefinitionInput = {
  id: string;
  title: string;
  description: string | null;
  isRequired: boolean;
  isUnique: boolean;
  isDefaultValueEnabled: boolean;
  defaultValue: string | null;
  isMultiselect: boolean;
  options: CustomFieldOption[] | null;
};

const BASE = "/api/v1/parties/custom-fields";

export function getCustomFieldDefinitions(params?: {
  entityType?: CustomFieldEntityType | null;
  includeInactive?: boolean;
}): Promise<CustomFieldDefinitionDto[]> {
  const qs = new URLSearchParams();
  if (params?.entityType) qs.set("EntityType", params.entityType);
  if (params?.includeInactive) qs.set("IncludeInactive", "true");
  const q = qs.toString();
  return apiFetch(q ? `${BASE}?${q}` : BASE);
}

export const createCustomFieldDefinition = (
  input: CreateCustomFieldDefinitionInput,
): Promise<string> => apiFetch(BASE, { method: "POST", body: JSON.stringify(input) });

export const updateCustomFieldDefinition = (
  input: UpdateCustomFieldDefinitionInput,
): Promise<void> => apiFetch(`${BASE}/${input.id}`, { method: "PUT", body: JSON.stringify(input) });

export const deleteCustomFieldDefinition = (id: string): Promise<void> =>
  apiFetch(`${BASE}/${id}`, { method: "DELETE" });
