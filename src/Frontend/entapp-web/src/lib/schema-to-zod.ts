import { z, ZodTypeAny } from "zod";
import type { FieldMetadata } from "@/types/dynamic";

/**
 * Metadata field listesinden Zod validation schema üretir.
 * DynamicForm'da React Hook Form + Zod entegrasyonu için kullanılır.
 */
export function metadataToZodSchema(
  fields: FieldMetadata[]
): z.ZodObject<Record<string, ZodTypeAny>> {
  const shape: Record<string, ZodTypeAny> = {};

  for (const field of fields) {
    if (field.readOnly || field.computed) continue;

    let schema: ZodTypeAny;

    switch (field.type) {
      case "boolean":
        schema = z.boolean();
        break;

      case "number":
      case "decimal":
      case "money": {
        let numSchema = z.coerce.number();
        if (field.min !== undefined && field.min !== null) {
          numSchema = numSchema.min(field.min);
        }
        if (field.max !== undefined && field.max !== null) {
          numSchema = numSchema.max(field.max);
        }
        schema = field.required ? numSchema : numSchema.optional();
        break;
      }

      case "enum":
        if (field.options && field.options.length > 0) {
          schema = z.enum(field.options as [string, ...string[]]);
        } else {
          schema = z.string();
        }
        if (!field.required) schema = schema.optional();
        break;

      case "date":
      case "datetime":
        schema = field.required
          ? z.string().min(1, `${field.label} zorunludur`)
          : z.string().optional();
        break;

      case "lookup":
        // Lookup → guid string
        schema = field.required
          ? z.string().uuid(`Geçerli bir seçim yapın`)
          : z.string().optional();
        break;

      default: {
        // string, text, richtext, file
        let strSchema = z.string();
        if (field.required) {
          strSchema = strSchema.min(1, `${field.label} zorunludur`);
        }
        if (field.minLength > 0) {
          strSchema = strSchema.min(
            field.minLength,
            `En az ${field.minLength} karakter`
          );
        }
        if (field.maxLength > 0) {
          strSchema = strSchema.max(
            field.maxLength,
            `En fazla ${field.maxLength} karakter`
          );
        }
        schema = field.required ? strSchema : strSchema.optional();
        break;
      }
    }

    shape[field.name] = schema;
  }

  return z.object(shape);
}

/**
 * Metadata field listesinden boş form default değerlerini üretir.
 */
export function metadataToDefaults(
  fields: FieldMetadata[]
): Record<string, unknown> {
  const defaults: Record<string, unknown> = {};

  for (const field of fields) {
    if (field.readOnly || field.computed) continue;

    if (field.defaultValue !== undefined && field.defaultValue !== null) {
      defaults[field.name] = parseDefaultValue(field);
    } else {
      defaults[field.name] = getEmptyDefault(field.type);
    }
  }

  return defaults;
}

function parseDefaultValue(field: FieldMetadata): unknown {
  const val = field.defaultValue!;
  switch (field.type) {
    case "boolean":
      return val === "true";
    case "number":
    case "decimal":
    case "money":
      return Number(val) || 0;
    default:
      return val;
  }
}

function getEmptyDefault(type: string): unknown {
  switch (type) {
    case "boolean":
      return false;
    case "number":
    case "decimal":
    case "money":
      return 0;
    default:
      return "";
  }
}
