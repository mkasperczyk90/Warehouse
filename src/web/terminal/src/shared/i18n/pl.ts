/** Polish catalogue — the terminal's default, for the floor staff. */
export const pl: Record<string, string> = {
  // app
  'app.name': 'Terminal WMS',

  // login (skan badge'a)
  'login.title': 'Zaloguj się',
  'login.subtitle': 'Zeskanuj swój identyfikator, aby rozpocząć zmianę',
  'login.badgePlaceholder': 'Zeskanuj identyfikator lub wpisz numer…',
  'login.error': 'Nie rozpoznano identyfikatora — spróbuj ponownie',
  'login.hint': 'Przyłóż identyfikator do czytnika lub wpisz numer i naciśnij Enter',

  // common
  'common.online': 'Online',
  'common.offline': 'Offline',
  'common.loading': 'Ładowanie…',
  'common.loadError': 'Nie udało się załadować — sprawdź połączenie.',
  'common.retry': 'Ponów',
  'common.cancel': 'Anuluj',

  // bottom nav
  'nav.tasks': 'Zadania',
  'nav.scan': 'Skanuj',
  'nav.lookup': 'Wyszukaj',
  'nav.more': 'Więcej',

  // accessibility labels
  'a11y.back': 'Wstecz',
  'a11y.toggleContrast': 'Przełącz tryb wysokiego kontrastu',
  'a11y.toggleLanguage': 'Zmień język',
  'a11y.toggleConnectivity': 'Przełącz połączenie',
  'a11y.clearSearch': 'Wyczyść wyszukiwanie',
  'a11y.decrease': 'zmniejsz',
  'a11y.increase': 'zwiększ',
  'a11y.enterQty': 'wpisz ilość',
  'a11y.backspace': 'cofnij',
  'a11y.confirmQty': 'potwierdź ilość',
  'a11y.signOut': 'Wyloguj się',

  // task hub
  'hub.tasksHeading': 'Twoje zadania',
  'hub.scanStart': 'Zeskanuj kod, aby rozpocząć…',
  'hub.offline': 'Praca offline — potwierdzenia są zapisywane na urządzeniu i zsynchronizują się po odzyskaniu sieci.',
  'hub.queued': '{n} w kolejce',

  // task tiles (by kind)
  'tasks.receive': 'Przyjęcie',
  'tasks.putaway': 'Odkładanie',
  'tasks.pick': 'Kompletacja',
  'tasks.move': 'Przesunięcie',

  // numeric keypad
  'keypad.expected': '= Oczekiwano · {n} {unit}',

  // goods receipt
  'receive.title': 'Przyjęcie towaru',
  'receive.line': 'Pozycja {n} z {total}',
  'receive.confirm': 'Potwierdź pozycję ✓',
  'receive.discrepancy': 'Zgłoś rozbieżność',
  'receive.scan': 'Zeskanuj kod towaru…',
  'receive.skuScanned': 'SKU {sku} · zeskanowano EAN ✓',
  'receive.expected': 'Oczekiwano wg awizacji',
  'receive.counted': 'Policzono',
  'receive.keyHint': 'Dotknij liczby, aby otworzyć klawiaturę',
  'receive.batch': 'Partia / nr lot',
  'receive.bbe': 'Najlepiej spożyć przed',
  'receive.keypadLabel': '{product} · policzono ({unit})',
  'receive.discrepancyTitle': 'Dlaczego liczba się różni?',
  'receive.reason.shortage': 'Niedobór — mniej niż oczekiwano',
  'receive.reason.overage': 'Nadwyżka — więcej niż oczekiwano',
  'receive.reason.damage': 'Towar uszkodzony',
  'receive.reason.damageHint': 'Kieruje partię na kwarantannę QC',

  // picking
  'pick.title': 'Kompletacja — fala {wave}',
  'pick.order': 'Zamówienie {order}',
  'pick.confirm': 'Potwierdź pobranie ✓',
  'pick.scanToConfirm': 'Zeskanuj, aby potwierdzić',
  'pick.short': 'Niedobór',
  'pick.left': 'pozostało {n}',
  'pick.goTo': 'Idź do',
  'pick.qty': 'Ilość do pobrania',
  'pick.scanLocation': 'Zeskanuj lokalizację',
  'pick.scanProduct': 'Zeskanuj produkt',
  'pick.promptLocation': 'Zeskanuj lokalizację…',
  'pick.promptProduct': 'Zeskanuj produkt…',
  'pick.scanned': 'Zeskanowano ✓',
  'pick.shortTitle': 'Dlaczego kompletacja jest niepełna?',
  'pick.reason.shortAtLocation': 'Mniej towaru w tej lokalizacji',
  'pick.reason.batchBlocked': 'Partia zablokowana / na QC',
  'pick.reason.batchBlockedHint': 'Przeplanuje na kolejną partię FEFO',
  'pick.reason.damaged': 'Uszkodzony w lokalizacji',

  // put-away
  'putaway.title': 'Odkładanie',
  'putaway.task': 'Zadanie {n} z {total}',
  'putaway.confirm': 'Potwierdź odłożenie ✓',
  'putaway.full': 'Lokalizacja pełna — zaproponuj inną',
  'putaway.lpn': 'Paleta LPN {lpn}',
  'putaway.proposed': 'Proponowana lokalizacja',
  'putaway.scan': 'Zeskanuj lokalizację, aby potwierdzić…',

  // move
  'move.title': 'Przesunięcie zapasu',
  'move.task': 'Zadanie uzupełnienia {n} z {total}',
  'move.confirm': 'Potwierdź przesunięcie ✓',
  'move.transfer': 'Przesunięcie międzymagazynowe → w drodze',
  'move.from': 'Z',
  'move.to': 'Do — zeskanuj cel',
  'move.skuBatch': 'SKU {sku} · {batch}',
  'move.qty': 'Ilość do przesunięcia',
  'move.scan': 'Zeskanuj lokalizację docelową…',

  // packing
  'pack.title': 'Pakowanie — {order}',
  'pack.customer': 'Klient: {customer}',
  'pack.close': 'Zamknij paczkę i drukuj etykietę',
  'pack.add': '+ Dodaj kolejną paczkę',
  'pack.active': 'Aktywna paczka',
  'pack.heading': 'Pobrane pozycje — zeskanuj do paczki',
  'pack.weight': 'Waga',
  'pack.dimensions': 'Wymiary',
  'pack.scan': 'Zeskanuj pozycję do paczki…',

  // scan dispatcher
  'scan.title': 'Skanowanie',
  'scan.subtitle': 'Zeskanuj cokolwiek — paletę, lokalizację, awizację, zamówienie lub produkt',
  'scan.placeholder': 'Naciśnij spust lub wpisz kod…',
  'scan.noAction': 'Brak akcji — spróbuj innego kodu.',
  'scan.recent': 'Ostatnio skanowane',
  'scan.unknownSub': 'Brak pasującej awizacji, zamówienia, produktu, palety ani lokalizacji',
  'scan.kind.asn': 'Awizacja przyjęcia (ASN)',
  'scan.kind.wave': 'Fala kompletacji',
  'scan.kind.order': 'Zamówienie wydania',
  'scan.kind.product': 'Produkt (EAN)',
  'scan.kind.lpn': 'Paleta (LPN)',
  'scan.kind.location': 'Lokalizacja',
  'scan.kind.unknown': 'Nierozpoznany kod',
  'scan.action.receive': 'Otwórz przyjęcie →',
  'scan.action.pick': 'Otwórz kompletację →',
  'scan.action.lookup': 'Sprawdź stan i ATP →',
  'scan.action.putaway': 'Odłóż tę paletę →',
  'scan.action.move': 'Przesuń zapas stąd →',

  // look up
  'lookup.title': 'Wyszukiwanie',
  'lookup.subtitle': 'Szukaj zapasów, lokalizacji i partii — tylko do odczytu',
  'lookup.placeholder': 'Szukaj SKU, nazwy, lokalizacji, partii…',
  'lookup.filter.all': 'Wszystko',
  'lookup.filter.product': 'Produkty',
  'lookup.filter.location': 'Lokalizacje',
  'lookup.filter.batch': 'Partie',
  'lookup.count': 'Wyniki: {n}',
  'lookup.empty': 'Brak wyników. Spróbuj innego hasła lub filtra.',

  // stock status (by StatusKey)
  'status.available': 'Dostępny',
  'status.reserved': 'Zarezerwowany',
  'status.blocked': 'Zablokowany (QC)',
  'status.expired': 'Przeterminowany',
  'status.transit': 'W drodze',
};
