import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';

import { en } from './en';
import { pl } from './pl';

// The domain was discovered in Polish (Wrocław); English is the default UI
// language, Polish a first-class second. Keys mirror the terminal's i18n.
void i18n.use(initReactI18next).init({
  resources: {
    en: { translation: en },
    pl: { translation: pl },
  },
  lng: 'en',
  fallbackLng: 'en',
  interpolation: { escapeValue: false },
});

export default i18n;
