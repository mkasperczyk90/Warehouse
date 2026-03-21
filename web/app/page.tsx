// import Image from "next/image";

// export default function Home() {
//   return (
//     <div className="flex min-h-screen items-center justify-center bg-zinc-50 font-sans dark:bg-black">
//       <main className="flex min-h-screen w-full max-w-3xl flex-col items-center justify-between py-32 px-16 bg-white dark:bg-black sm:items-start">
//         <Image
//           className="dark:invert"
//           src="/next.svg"
//           alt="Next.js logo"
//           width={100}
//           height={20}
//           priority
//         />
//         <div className="flex flex-col items-center gap-6 text-center sm:items-start sm:text-left">
//           <h1 className="max-w-xs text-3xl font-semibold leading-10 tracking-tight text-black dark:text-zinc-50">
//             To get started, edit the page.tsx file.
//           </h1>
//           <p className="max-w-md text-lg leading-8 text-zinc-600 dark:text-zinc-400">
//             Looking for a starting point or more instructions? Head over to{" "}
//             <a
//               href="https://vercel.com/templates?framework=next.js&utm_source=create-next-app&utm_medium=appdir-template-tw&utm_campaign=create-next-app"
//               className="font-medium text-zinc-950 dark:text-zinc-50"
//             >
//               Templates
//             </a>{" "}
//             or the{" "}
//             <a
//               href="https://nextjs.org/learn?utm_source=create-next-app&utm_medium=appdir-template-tw&utm_campaign=create-next-app"
//               className="font-medium text-zinc-950 dark:text-zinc-50"
//             >
//               Learning
//             </a>{" "}
//             center.
//           </p>
//         </div>
//         <div className="flex flex-col gap-4 text-base font-medium sm:flex-row">
//           <a
//             className="flex h-12 w-full items-center justify-center gap-2 rounded-full bg-foreground px-5 text-background transition-colors hover:bg-[#383838] dark:hover:bg-[#ccc] md:w-[158px]"
//             href="https://vercel.com/new?utm_source=create-next-app&utm_medium=appdir-template-tw&utm_campaign=create-next-app"
//             target="_blank"
//             rel="noopener noreferrer"
//           >
//             <Image
//               className="dark:invert"
//               src="/vercel.svg"
//               alt="Vercel logomark"
//               width={16}
//               height={16}
//             />
//             Deploy Now
//           </a>
//           <a
//             className="flex h-12 w-full items-center justify-center rounded-full border border-solid border-black/[.08] px-5 transition-colors hover:border-transparent hover:bg-black/[.04] dark:border-white/[.145] dark:hover:bg-[#1a1a1a] md:w-[158px]"
//             href="https://nextjs.org/docs?utm_source=create-next-app&utm_medium=appdir-template-tw&utm_campaign=create-next-app"
//             target="_blank"
//             rel="noopener noreferrer"
//           >
//             Documentation
//           </a>
//         </div>
//       </main>
//     </div>
//   );
// }

'use client';

import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';

export default function Home() {
  const [user, setUser] = useState<{ username: string; roles: string[] } | null>(null);
  const [loading, setLoading] = useState(true);

  // Sprawdzamy, czy użytkownik jest zalogowany odpytując Gateway
  useEffect(() => {
    const fetchUser = async () => {
      try {
        const res = await fetch('http://localhost:5000/api/auth/me', {
          // KLUCZOWE: wymusza dołączenie ciastka autoryzacyjnego do zapytania!
          credentials: 'include', 
        });
        
        if (res.ok) {
          const data = await res.json();
          setUser(data);
        }
      } catch (error) {
        console.error("Błąd podczas sprawdzania sesji", error);
      } finally {
        setLoading(false);
      }
    };

    fetchUser();
  }, []);

  // Funkcje nawigujące do endpointów BFF
  const login = () => {
    window.location.href = 'http://localhost:5000/api/auth/login';
  };

  const logout = () => {
    window.location.href = 'http://localhost:5000/api/auth/logout';
  };

  const fetchProducts = async () => {
    // Odpytujemy YARP, który pod spodem uderzy do Product.Api z tokenem JWT!
    const res = await fetch('http://localhost:5000/api/products', {
      credentials: 'include'
    });
    const data = await res.json();
    console.log("Produkty:", data);
  };

  if (loading) return <div className="p-8">Ładowanie...</div>;

  return (
    <main className="p-8 flex flex-col items-start gap-4">
      <h1 className="text-2xl font-bold">Warehouse Storefront</h1>

      {!user ? (
        <div className="flex flex-col gap-4 border p-6 rounded-lg bg-slate-50">
          <p>Nie jesteś zalogowany.</p>
          <Button onClick={login}>Zaloguj przez Keycloak</Button>
        </div>
      ) : (
        <div className="flex flex-col gap-4 border p-6 rounded-lg bg-green-50">
          <p>Witaj, <strong>{user.username}</strong>!</p>
          <p className="text-sm text-gray-600">
            Twoje role: {user.roles.join(', ')}
          </p>
          
          <div className="flex gap-4 mt-4">
            <Button variant="outline" onClick={fetchProducts}>
              Pobierz Produkty z API (Test Gatewaya)
            </Button>
            <Button variant="destructive" onClick={logout}>
              Wyloguj
            </Button>
          </div>
        </div>
      )}
    </main>
  );
}