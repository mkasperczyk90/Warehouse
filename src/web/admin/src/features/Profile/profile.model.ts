import { useMutation, useQuery } from '@tanstack/react-query';

import { api } from '@/core/api/client';
import type { UserRole } from '@/features/Auth';

/** A past sign-in, shown on the profile's activity panel. */
export interface ProfileSession {
  id: string;
  when: string;
  device: string;
}

/** The full desk-user record behind the avatar (identity + editable prefs). */
export interface UserProfile {
  id: string;
  badge: string;
  name: string;
  role: UserRole;
  email: string;
  phone: string;
  defaultWarehouseId: string;
  language: 'en' | 'pl';
  lastLogin: string;
  recentSessions: ProfileSession[];
}

/** The fields the profile screen lets the user change. */
export interface ProfilePrefs {
  phone: string;
  defaultWarehouseId: string;
  language: 'en' | 'pl';
}

export function useProfile(id: string | undefined) {
  return useQuery({
    queryKey: ['profile', id],
    queryFn: () => api.get<UserProfile>(`profile/${id}`),
    enabled: !!id,
  });
}

export function useUpdateProfile(id: string | undefined) {
  return useMutation({
    mutationFn: (body: ProfilePrefs) => api.post<UserProfile>(`profile/${id}`, body),
  });
}
