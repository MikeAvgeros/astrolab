// Minimal OpenAPI 3.1 reader: just enough to generate forms for AstroLab's operations.

export interface Schema {
  $ref?: string;
  type?: string | string[];
  format?: string;
  enum?: string[];
  default?: unknown;
  items?: Schema;
  properties?: Record<string, Schema>;
  required?: string[];
}

export interface Parameter {
  name: string;
  in: 'path' | 'query' | 'header' | 'cookie';
  required?: boolean;
  schema?: Schema;
}

interface RawOperation {
  tags?: string[];
  summary?: string;
  operationId?: string;
  parameters?: Parameter[];
  requestBody?: { content?: Record<string, { schema?: Schema }> };
}

export interface OpenApiSpec {
  paths: Record<string, Record<string, RawOperation>>;
  components?: { schemas?: Record<string, Schema> };
}

export interface Operation {
  key: string;
  method: string;
  path: string;
  tag: string;
  summary?: string;
  parameters: Parameter[];
  bodySchema?: Schema;
  rawBody: boolean;
}

const HTTP_METHODS = ['get', 'post', 'put', 'patch', 'delete'];

export async function loadSpec(): Promise<OpenApiSpec> {
  const res = await fetch('/openapi/v1.json');

  if (!res.ok) throw new Error(`Failed to load OpenAPI document (HTTP ${res.status}).`);

  return res.json();
}

export function listOperations(spec: OpenApiSpec): Operation[] {
  const ops: Operation[] = [];

  for (const [path, item] of Object.entries(spec.paths)) {
    for (const [method, op] of Object.entries(item)) {
      if (!HTTP_METHODS.includes(method)) continue;

      ops.push({
        key: `${method.toUpperCase()} ${path}`,
        method: method.toUpperCase(),
        path,
        tag: op.tags?.[0] ?? 'Other',
        summary: op.summary,
        parameters: op.parameters ?? [],
        bodySchema: op.requestBody?.content?.['application/json']?.schema,
        // The upload endpoint reads the raw request body and therefore declares no body schema.
        rawBody: method === 'post' && path.endsWith('/upload'),
      });
    }
  }

  return ops;
}

export function resolve(spec: OpenApiSpec, schema: Schema | undefined): Schema {
  if (!schema) return {};

  if (schema.$ref) {
    const name = schema.$ref.split('/').pop()!;
    const target = spec.components?.schemas?.[name] ?? {};

    // Keep sibling keywords such as "default" that ASP.NET emits alongside $ref.
    return { ...resolve(spec, target), ...withoutRef(schema) };
  }

  return schema;
}

function withoutRef(schema: Schema): Schema {
  const { $ref: _ref, ...rest } = schema;

  return rest;
}

/** ASP.NET emits numeric types as ["number", "string"]; pick the meaningful one. */
export function primaryType(schema: Schema): string {
  if (schema.enum) return 'enum';

  const types = Array.isArray(schema.type) ? schema.type : schema.type ? [schema.type] : [];

  return types.find(t => t !== 'string' && t !== 'null') ?? types[0] ?? 'string';
}

export function enumValues(spec: OpenApiSpec, name: string): string[] {
  return spec.components?.schemas?.[name]?.enum ?? [];
}

/** Builds an example JSON body containing the required properties, pre-filling file IDs. */
export function buildTemplate(spec: OpenApiSpec, schema: Schema | undefined, fileId: string, depth = 0): unknown {
  const s = resolve(spec, schema);

  if (depth > 5) return null;

  switch (primaryType(s)) {
    case 'enum':
      return s.enum![0];
    case 'number':
    case 'integer':
      return typeof s.default === 'number' ? s.default : 0;
    case 'boolean':
      return false;
    case 'array':
      return [buildTemplate(spec, s.items, fileId, depth + 1)];
    case 'object': {
      const result: Record<string, unknown> = {};
      for (const name of s.required ?? []) {
        if (/fileId$/i.test(name)) result[name] = fileId;
        else if (/fileIds$/i.test(name)) result[name] = [fileId];
        else result[name] = buildTemplate(spec, s.properties?.[name], fileId, depth + 1);
      }
      return result;
    }
    default:
      return '';
  }
}

export function describeType(spec: OpenApiSpec, schema: Schema | undefined): string {
  const s = resolve(spec, schema);
  const type = primaryType(s);

  if (type === 'enum') return s.enum!.join(' | ');
  if (type === 'array') return `${describeType(spec, s.items)}[]`;
  if (schema?.$ref) return schema.$ref.split('/').pop()!;

  return type;
}
