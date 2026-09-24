import { useEffect, useState } from 'react';
import { ArchiveSearch } from './components/ArchiveSearch';
import { Explorer } from './components/Explorer';
import { FilesPanel } from './components/FilesPanel';
import { ImageViewer } from './components/ImageViewer';
import { useStagedFiles } from './files';
import { loadSpec, type OpenApiSpec } from './openapi';

type Tab = 'viewer' | 'archives' | 'explorer';

const TABS: { id: Tab; label: string }[] = [
  { id: 'viewer', label: 'Image viewer' },
  { id: 'archives', label: 'Archive search' },
  { id: 'explorer', label: 'All endpoints' },
];

function tabFromHash(): Tab {
  const hash = window.location.hash.slice(1);

  return TABS.some(t => t.id === hash) ? (hash as Tab) : 'viewer';
}

export function App() {
  const [spec, setSpec] = useState<OpenApiSpec | null>(null);
  const [specError, setSpecError] = useState<string | null>(null);
  const [tab, setTab] = useState<Tab>(tabFromHash);
  const { files, add, remove } = useStagedFiles();
  const [activeFileId, setActiveFileId] = useState(() => files[0]?.fileId ?? '');

  useEffect(() => {
    loadSpec().then(setSpec, (err: Error) => setSpecError(err.message));
  }, []);

  useEffect(() => {
    const onHash = () => setTab(tabFromHash());
    window.addEventListener('hashchange', onHash);
    return () => window.removeEventListener('hashchange', onHash);
  }, []);

  const activeFile = files.find(f => f.fileId === activeFileId);

  return (
    <div className="app">
      <header className="app-header">
        <h1>AstroLab</h1>
        <nav className="tabs">
          {TABS.map(t => (
            <a key={t.id} href={`#${t.id}`} className={tab === t.id ? 'active' : ''}>{t.label}</a>
          ))}
        </nav>
        <span className="active-file muted">
          {activeFileId ? <>Active: <strong>{activeFile?.label ?? activeFileId}</strong></> : 'No active file'}
        </span>
      </header>

      <div className="app-body">
        <FilesPanel
          files={files}
          activeFileId={activeFileId}
          onSelect={setActiveFileId}
          onAdd={add}
          onRemove={id => {
            remove(id);
            if (id === activeFileId) setActiveFileId('');
          }}
        />

        <main className="app-main">
          {specError && (
            <div className="banner error">
              <strong>Cannot reach the AstroLab API.</strong> {specError}
              <p>
                Start it with <code>docker compose up -d</code> or <code>dotnet run --project src/AstroLab.Api</code>,
                or point the proxy at a running API with <code>ASTROLAB_API_URL</code>.
              </p>
            </div>
          )}
          {!spec && !specError && <p className="muted">Loading API description…</p>}
          {spec && tab === 'viewer' && <ImageViewer spec={spec} fileId={activeFileId} />}
          {spec && tab === 'archives' && (
            <ArchiveSearch
              spec={spec}
              onFileStaged={add}
              onOpen={id => {
                setActiveFileId(id);
                window.location.hash = 'viewer';
              }}
            />
          )}
          {spec && tab === 'explorer' && (
            <Explorer
              spec={spec}
              files={files}
              activeFileId={activeFileId}
              onFileStaged={file => {
                add(file);
                setActiveFileId(file.fileId);
              }}
            />
          )}
        </main>
      </div>
    </div>
  );
}
