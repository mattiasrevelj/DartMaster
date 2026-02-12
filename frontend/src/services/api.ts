import axios from 'axios'

const API_BASE_URL = 'http://localhost:5146/api'

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json'
  }
})

// Add auth token to requests
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

export interface LoginRequest {
  username: string
  password: string
}

export interface RegisterRequest {
  username: string
  email: string
  password: string
  fullName: string
}

export interface LoginResponse {
  success: boolean
  data?: {
    userId: string
    username: string
    email: string
    fullName: string
    token: string
    refreshToken: string
  }
  message?: string
}

export interface Tournament {
  id: string
  name: string
  description?: string
  status: string
  startDate: string
  maxPlayers: number
  adminName: string
  participantCount: number
}

export interface Match {
  id: string
  tournamentId: string
  status: string
  matchFormat: string
  participantsCount: number
}

export const authAPI = {
  login: (data: LoginRequest) => api.post<LoginResponse>('/users/login', data),
  register: (data: RegisterRequest) => api.post<LoginResponse>('/users/register', data),
}

export const tournamentAPI = {
  getAll: () => api.get<{ success: boolean; data: Tournament[] }>('/tournaments'),
  getById: (id: string) => api.get<{ success: boolean; data: Tournament }>(`/tournaments/${id}`),
  create: (data: any) => api.post('/tournaments', data),
  update: (id: string, data: any) => api.put(`/tournaments/${id}`, data),
  delete: (id: string) => api.delete(`/tournaments/${id}`),
}

export const matchAPI = {
  getTournamentMatches: (tournamentId: string) => 
    api.get<{ success: boolean; data: Match[] }>(`/matches?tournamentId=${tournamentId}`),
  getById: (id: string) => api.get<{ success: boolean; data: Match }>(`/matches/${id}`),
  create: (data: any) => api.post('/matches', data),
  addParticipant: (matchId: string, userId: string) => 
    api.post(`/matches/${matchId}/participants`, { userId }),
}

export default api
