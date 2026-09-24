import { useState } from 'react';
import { callApi, type ApiResponse, type QueryValue } from '../api';
import type { StagedFile } from '../files';
import { buildTemplate, describeType, primaryType, resolve, type OpenApiSpec, type Operation, type Parameter } from '../openapi';
import { ResponseView } from './ResponseView';

interface Props {
  spec: OpenApiSpec;
  operation: Operation;
  files: StagedFile[];
  activeFileId: string;
  onFileStaged: (file: Omit<StagedFile, 'addedAt'>) => void;
}

export function OperationForm({ spec, operation, files, activeFileId, onFileStaged }: Props) {
  // The parent keys this component by operation, so initial state is rebuilt per operation.
  const [values, setValues] = useState<Record<string, string>>(() =>
    Object.fromEntries(operation.parameters.filter(p => p.name === 'fileId').map(p => [p.name, activeFileId])));
  const [body, setBody] = useState(() =>
    operation.bodySchema ? JSON.stringify(buildTemplate(spec, operation.bodySchema, activeFileId), null, 2) : '');
  const [rawFile, setRawFile] = useState<File | null>(null);
  const [response, setResponse] = useState<ApiResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [busy, setBusy] = useState(false);

  const bodySchema = operation.bodySchema ? resolve(spec, operation.bodySchema) : undefined;
  const optionalProps = Object.entries(bodySchema?.properties ?? {}).filter(([n]) => !bodySchema?.required?.includes(n));

  async function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);

    let path = operation.path;
    const query: Record<string, QueryValue> = {};

    for (const p of operation.parameters) {
      const raw = values[p.name]?.trim() ?? '';

      if (p.in === 'path') {
        if (!raw) return setError(`Path parameter "${p.name}" is required.`);
        path = path.replace(`{${p.name}}`, encodeURIComponent(raw));
      } else if (p.in === 'query' && raw) {
        query[p.name] = primaryType(resolve(spec, p.schema)) === 'array'
          ? raw.split(',').map(s => s.trim()).filter(Boolean)
          : raw;
      }
    }

    let json: unknown;

    if (operation.bodySchema) {
      try {
        json = JSON.parse(body);
      } catch (err) {
        return setError(`Request body is not valid JSON: ${(err as Error).message}`);
      }
    }

    if (operation.rawBody && !rawFile) return setError('Choose a FITS file to upload.');

    setBusy(true);
    try {
      const res = await callApi(operation.method, path, { query, json, rawBody: rawFile ?? undefined });
      setResponse(res);
      trackStagedFile(res);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setBusy(false);
    }
  }

  // Upload and archive download both return a new fileId; remember it in the file list.
  function trackStagedFile(res: ApiResponse) {
    const payload = res.json as { fileId?: string; sizeBytes?: number } | undefined;

    if (!res.ok || !payload?.fileId) return;

    if (operation.rawBody) {
      onFileStaged({ fileId: payload.fileId, label: rawFile?.name ?? payload.fileId, sizeBytes: payload.sizeBytes, source: 'upload' });
    } else if (operation.path === '/api/archives/download') {
      const datasetId = (JSON.parse(body) as { datasetId?: string }).datasetId ?? payload.fileId;
      onFileStaged({ fileId: payload.fileId, label: datasetId, sizeBytes: payload.sizeBytes, source: 'archive' });
    }
  }

  return (
    <div className="operation">
      <h2>
        <span className={`method method-${operation.method.toLowerCase()}`}>{operation.method}</span> <code>{operation.path}</code>
      </h2>
      {operation.summary && <p className="muted">{operation.summary}</p>}

      <form onSubmit={submit} className="op-form">
        {operation.parameters.map(p => (
          <ParameterInput
            key={p.name}
            spec={spec}
            param={p}
            files={files}
            value={values[p.name] ?? ''}
            onChange={v => setValues(prev => ({ ...prev, [p.name]: v }))}
          />
        ))}

        {operation.rawBody && (
          <label className="field">
            <span>FITS file <em className="required">*</em></span>
            <input type="file" accept=".fits,.fit,.fts,.fz,.gz" onChange={e => setRawFile(e.target.files?.[0] ?? null)} />
          </label>
        )}

        {operation.bodySchema && (
          <div className="field">
            <span>Request body (JSON) <small className="muted">{describeType(spec, operation.bodySchema)}</small></span>
            <textarea value={body} onChange={e => setBody(e.target.value)} rows={Math.min(18, body.split('\n').length + 2)} spellCheck={false} />
            {optionalProps.length > 0 && (
              <small className="muted">
                Optional: {optionalProps.map(([n, s]) => `${n} (${describeType(spec, s)})`).join(', ')}
              </small>
            )}
          </div>
        )}

        <div className="actions">
          <button type="submit" disabled={busy}>{busy ? 'Sending…' : 'Send request'}</button>
          {error && <span className="error-text">{error}</span>}
        </div>
      </form>

      {response && <ResponseView response={response} />}
    </div>
  );
}

interface ParameterInputProps {
  spec: OpenApiSpec;
  param: Parameter;
  files: StagedFile[];
  value: string;
  onChange: (value: string) => void;
}

function ParameterInput({ spec, param, files, value, onChange }: ParameterInputProps) {
  const schema = resolve(spec, param.schema);
  const type = primaryType(schema);
  const required = param.in === 'path' || param.required;
  const placeholder = schema.default !== undefined ? `default: ${String(schema.default)}` : type === 'array' ? 'comma-separated' : '';

  let input: React.ReactNode;

  if (param.name === 'fileId') {
    input = (
      <>
        <input list="staged-file-ids" value={value} onChange={e => onChange(e.target.value)} placeholder="file ID" />
        <datalist id="staged-file-ids">
          {files.map(f => <option key={f.fileId} value={f.fileId}>{f.label}</option>)}
        </datalist>
      </>
    );
  } else if (type === 'enum') {
    input = (
      <select value={value} onChange={e => onChange(e.target.value)}>
        <option value="">{schema.default !== undefined ? `(default: ${String(schema.default)})` : '(none)'}</option>
        {schema.enum!.map(v => <option key={v}>{v}</option>)}
      </select>
    );
  } else {
    const inputType = type === 'number' || type === 'integer' ? 'number' : schema.format === 'date-time' ? 'datetime-local' : 'text';
    input = (
      <input
        type={inputType}
        step={type === 'integer' ? 1 : 'any'}
        value={value}
        placeholder={placeholder}
        onChange={e => onChange(e.target.value)}
      />
    );
  }

  return (
    <label className="field">
      <span>
        {param.name} {required && <em className="required">*</em>} <small className="muted">{param.in} · {describeType(spec, param.schema)}</small>
      </span>
      {input}
    </label>
  );
}
