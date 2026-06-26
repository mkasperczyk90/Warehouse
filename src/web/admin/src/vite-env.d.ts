/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** When 'false', the MSW worker is not started and the app calls the real Gateway. Build-time only. */
  readonly VITE_USE_MOCKS?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
