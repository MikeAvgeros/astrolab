import { useState } from 'react';
import { describeError, type ApiResponse } from '../api';
import { Plot, type Series } from './Plot';

export function ResponseView({ response }: { response: ApiResponse }) {
  const [showRaw, setShowRaw] = useState(false);
  const series: Series = {};
  const tableSeries: Series = {};
  if (response.json !== undefined) extractSeries(response.json, series, tableSeries);
  const hasSeries = Object.keys(series).length > 0;
  const hasTableSeries = Object.keys(tableSeries).length > 0;
  const tables = response.json !== undefined ? extractTables(response.json) : [];

  return (
    <div className="response">
      <div className={`status ${response.ok ? 'ok' : 'error'}`}>
        <strong>{response.status}</strong> {response.method} {response.url}
        <span className="muted"> · {response.durationMs} ms · {response.contentType || 'no content'}</span>
      </div>

      {!response.ok && <p className="error-text">{describeError(response)}</p>}

      {response.blobUrl && response.contentType.startsWith('image/') && (
        <figure className="image-result">
          <img src={response.blobUrl} alt="API image response" />
          <figcaption><a href={response.blobUrl} download="astrolab.png">Download image</a></figcaption>
        </figure>
      )}

      {response.blobUrl && !response.contentType.startsWith('image/') && (
        <a href={response.blobUrl} download>Download response</a>
      )}

      {response.ok && hasSeries && <Plot key={response.url + response.durationMs} series={{ ...series, ...tableSeries }} />}

      {response.ok && !hasSeries && hasTableSeries && (
        <details className="table-block">
          <summary>Plot columns</summary>
          <Plot key={response.url + response.durationMs} series={tableSeries} initialMode="points" />
        </details>
      )}

      {response.ok && tables.map(t => <DataTable key={t.name} name={t.name} rows={t.rows} />)}

      {response.json !== undefined && response.ok && <ScalarSummary value={response.json} />}

      {(response.json !== undefined || response.text) && (
        <div>
          <button type="button" className="link" onClick={() => setShowRaw(s => !s)}>
            {showRaw ? 'Hide' : 'Show'} raw response
          </button>
          {showRaw && (
            <pre className="raw">{response.json !== undefined ? JSON.stringify(response.json, null, 2) : response.text}</pre>
          )}
        </div>
      )}
    </div>
  );
}

function ScalarSummary({ value }: { value: unknown }) {
  if (!isObject(value)) return null;

  const entries = Object.entries(value).filter(([, v]) => v === null || typeof v !== 'object');

  if (entries.length === 0) return null;

  const nested = Object.entries(value).filter(([, v]) => isObject(v)) as [string, Record<string, unknown>][];

  return (
    <div className="summary">
      <dl>
        {entries.map(([k, v]) => (
          <div key={k}>
            <dt>{k}</dt>
            <dd>{formatValue(v)}</dd>
          </div>
        ))}
      </dl>
      {nested.map(([k, v]) => (
        <details key={k}>
          <summary>{k}</summary>
          <ScalarSummary value={v} />
        </details>
      ))}
    </div>
  );
}

function DataTable({ name, rows }: { name: string; rows: Record<string, unknown>[] }) {
  const columns = Array.from(new Set(rows.flatMap(r => Object.keys(r))));
  const limit = 500;

  return (
    <details className="table-block" open={rows.length <= 50}>
      <summary>{name} ({rows.length} rows{rows.length > limit ? `, showing first ${limit}` : ''})</summary>
      <div className="table-scroll">
        <table>
          <thead>
            <tr>{columns.map(c => <th key={c}>{c}</th>)}</tr>
          </thead>
          <tbody>
            {rows.slice(0, limit).map((row, i) => (
              <tr key={i}>{columns.map(c => <td key={c}>{formatValue(row[c])}</td>)}</tr>
            ))}
          </tbody>
        </table>
      </div>
    </details>
  );
}

function formatValue(v: unknown): string {
  if (v === null || v === undefined) return '—';
  if (typeof v === 'number') return Number.isInteger(v) ? String(v) : Number(v.toPrecision(8)).toString();
  if (typeof v === 'object') return JSON.stringify(v);

  return String(v);
}

function isObject(v: unknown): v is Record<string, unknown> {
  return typeof v === 'object' && v !== null && !Array.isArray(v);
}

function isNumericArray(v: unknown): v is number[] {
  return Array.isArray(v) && v.length > 1 && v.every(x => typeof x === 'number' || x === null);
}

/**
 * Finds numeric arrays (e.g. flux, wavelength) and numeric columns of object arrays (e.g. detected sources)
 * up to two levels deep. They are kept apart because only the former are sensible to plot by default.
 */
function extractSeries(value: unknown, series: Series, tableSeries: Series, prefix = '', depth = 0) {
  if (depth > 2) return;

  if (isNumericArray(value)) {
    series[prefix || 'values'] = value.map(x => (x === null ? NaN : x));
  } else if (Array.isArray(value) && value.length > 1 && value.every(isObject)) {
    for (const key of Object.keys(value[0])) {
      if (value.every(row => typeof row[key] === 'number' || row[key] === null)) {
        tableSeries[prefix ? `${prefix}.${key}` : key] = value.map(row => (row[key] === null ? NaN : (row[key] as number)));
      }
    }
  } else if (isObject(value)) {
    for (const [k, v] of Object.entries(value)) extractSeries(v, series, tableSeries, prefix ? `${prefix}.${k}` : k, depth + 1);
  }
}

function extractTables(value: unknown, prefix = '', depth = 0, out: { name: string; rows: Record<string, unknown>[] }[] = []) {
  if (depth > 2) return out;

  if (Array.isArray(value) && value.length > 0 && value.every(isObject)) {
    out.push({ name: prefix || 'items', rows: value });
  } else if (isObject(value)) {
    for (const [k, v] of Object.entries(value)) extractTables(v, prefix ? `${prefix}.${k}` : k, depth + 1, out);
  }

  return out;
}
