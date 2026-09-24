import { useState } from 'react';
import { callApi, describeError } from '../api';
import { formatBytes, type StagedFile } from '../files';

interface Props {
  files: StagedFile[];
  activeFileId: string;
  onSelect: (fileId: string) => void;
  onAdd: (file: Omit<StagedFile, 'addedAt'>) => void;
  onRemove: (fileId: string) => void;
}

export function FilesPanel({ files, activeFileId, onSelect, onAdd, onRemove }: Props) {
  const [uploading, setUploading] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [manualId, setManualId] = useState('');

  async function upload(file: File) {
    setError(null);
    setUploading(file.name);
    try {
      const res = await callApi('POST', '/api/fits/upload', { rawBody: file });
      const payload = res.json as { fileId: string; sizeBytes: number } | undefined;

      if (!res.ok || !payload) {
        setError(describeError(res));
        return;
      }

      onAdd({ fileId: payload.fileId, label: file.name, sizeBytes: payload.sizeBytes, source: 'upload' });
      onSelect(payload.fileId);
    } catch (err) {
      setError((err as Error).message);
    } finally {
      setUploading(null);
    }
  }

  function addManual(e: React.FormEvent) {
    e.preventDefault();
    const id = manualId.trim();

    if (!id) return;

    onAdd({ fileId: id, label: id, source: 'manual' });
    onSelect(id);
    setManualId('');
  }

  return (
    <aside className="files-panel">
      <h2>Staged files</h2>

      <label className="upload-button">
        {uploading ? `Uploading ${uploading}…` : 'Upload FITS file'}
        <input
          type="file"
          accept=".fits,.fit,.fts,.fz,.gz"
          disabled={uploading !== null}
          onChange={e => {
            const file = e.target.files?.[0];
            e.target.value = '';
            if (file) void upload(file);
          }}
        />
      </label>
      {error && <p className="error-text">{error}</p>}

      {files.length === 0 && <p className="muted">No files yet. Upload one, or download from an archive.</p>}

      <ul className="file-list">
        {files.map(f => (
          <li key={f.fileId} className={f.fileId === activeFileId ? 'active' : ''}>
            <button type="button" className="file-select" onClick={() => onSelect(f.fileId)} title={f.fileId}>
              <span className="file-label">{f.label}</span>
              <span className="muted file-meta">{f.source}{f.sizeBytes !== undefined ? ` · ${formatBytes(f.sizeBytes)}` : ''}</span>
              <code className="file-id">{f.fileId}</code>
            </button>
            <div className="file-actions">
              <button type="button" className="link" onClick={() => void navigator.clipboard?.writeText(f.fileId)}>Copy ID</button>
              <button type="button" className="link" onClick={() => onRemove(f.fileId)} title="Forget this file in the browser (does not delete it from the server)">Forget</button>
            </div>
          </li>
        ))}
      </ul>

      <form onSubmit={addManual} className="manual-id">
        <input value={manualId} onChange={e => setManualId(e.target.value)} placeholder="Add existing file ID" />
        <button type="submit">Add</button>
      </form>
    </aside>
  );
}
