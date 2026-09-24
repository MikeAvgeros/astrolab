import { useMemo, useState } from 'react';
import type { StagedFile } from '../files';
import { listOperations, type OpenApiSpec } from '../openapi';
import { OperationForm } from './OperationForm';

interface Props {
  spec: OpenApiSpec;
  files: StagedFile[];
  activeFileId: string;
  onFileStaged: (file: Omit<StagedFile, 'addedAt'>) => void;
}

/** Lists every operation in the API's OpenAPI document and generates a form for the selected one. */
export function Explorer({ spec, files, activeFileId, onFileStaged }: Props) {
  const operations = useMemo(() => listOperations(spec), [spec]);
  const [selectedKey, setSelectedKey] = useState(operations[0]?.key ?? '');
  const [filter, setFilter] = useState('');

  const visible = operations.filter(op =>
    `${op.key} ${op.summary ?? ''}`.toLowerCase().includes(filter.trim().toLowerCase()));
  const groups = Map.groupBy(visible, op => op.tag);
  const selected = operations.find(op => op.key === selectedKey);

  return (
    <div className="explorer">
      <nav className="op-list" aria-label="Endpoints">
        <input type="search" placeholder={`Filter ${operations.length} endpoints…`} value={filter} onChange={e => setFilter(e.target.value)} />
        {[...groups].map(([tag, ops]) => (
          <details key={tag} open>
            <summary>{tag} <span className="muted">({ops.length})</span></summary>
            <ul>
              {ops.map(op => (
                <li key={op.key}>
                  <button
                    type="button"
                    className={`op-item ${op.key === selectedKey ? 'selected' : ''}`}
                    onClick={() => setSelectedKey(op.key)}
                    title={op.summary}
                  >
                    <span className={`method method-${op.method.toLowerCase()}`}>{op.method}</span>
                    <span className="op-path">{op.path.replace(/^\/api\/[^/]+/, '').replace('/{fileId}', '') || '/'}</span>
                  </button>
                </li>
              ))}
            </ul>
          </details>
        ))}
      </nav>
      <section className="op-detail">
        {selected
          ? <OperationForm key={selected.key} spec={spec} operation={selected} files={files} activeFileId={activeFileId} onFileStaged={onFileStaged} />
          : <p className="muted">Select an endpoint.</p>}
      </section>
    </div>
  );
}
