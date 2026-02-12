import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { tournamentAPI, Tournament, matchAPI, Match } from '../services/api'
import '../styles/Dashboard.css'

export default function DashboardPage() {
  const [tournaments, setTournaments] = useState<Tournament[]>([])
  const [selectedTournament, setSelectedTournament] = useState<Tournament | null>(null)
  const [matches, setMatches] = useState<Match[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [showCreateForm, setShowCreateForm] = useState(false)
  const [newTournament, setNewTournament] = useState({ 
    name: '', 
    description: '', 
    startDate: '',
    maxPlayers: 16 
  })
  const navigate = useNavigate()
  const username = localStorage.getItem('username')

  useEffect(() => {
    fetchTournaments()
  }, [])

  const fetchTournaments = async () => {
    try {
      setLoading(true)
      const response = await tournamentAPI.getAll()
      if (response.data.success) {
        setTournaments(response.data.data || [])
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load tournaments')
    } finally {
      setLoading(false)
    }
  }

  const handleSelectTournament = async (tournament: Tournament) => {
    setSelectedTournament(tournament)
    try {
      const response = await matchAPI.getTournamentMatches(tournament.id)
      if (response.data.success) {
        setMatches(response.data.data || [])
      }
    } catch (err: any) {
      setError(err.message || 'Failed to load matches')
    }
  }

  const handleCreateTournament = async (e: React.FormEvent) => {
    e.preventDefault()
    try {
      await tournamentAPI.create(newTournament)
      setShowCreateForm(false)
      setNewTournament({ name: '', description: '', startDate: '', maxPlayers: 16 })
      fetchTournaments()
    } catch (err: any) {
      setError(err.response?.data?.message || 'Failed to create tournament')
    }
  }

  const handleLogout = () => {
    localStorage.removeItem('token')
    localStorage.removeItem('userId')
    localStorage.removeItem('username')
    navigate('/login')
  }

  return (
    <div className="dashboard">
      <nav className="navbar">
        <div className="navbar-brand">🎯 DartMaster</div>
        <div className="navbar-user">
          <span>Welcome, {username}!</span>
          <button onClick={handleLogout} className="logout-btn">Logout</button>
        </div>
      </nav>

      <div className="dashboard-container">
        <div className="tournaments-section">
          <div className="section-header">
            <h2>Tournaments</h2>
            <button 
              onClick={() => setShowCreateForm(!showCreateForm)} 
              className="create-btn"
            >
              + New Tournament
            </button>
          </div>

          {showCreateForm && (
            <form onSubmit={handleCreateTournament} className="create-form">
              <input
                type="text"
                placeholder="Tournament Name"
                value={newTournament.name}
                onChange={(e) => setNewTournament({...newTournament, name: e.target.value})}
                required
              />
              <input
                type="text"
                placeholder="Description"
                value={newTournament.description}
                onChange={(e) => setNewTournament({...newTournament, description: e.target.value})}
              />
              <input
                type="datetime-local"
                value={newTournament.startDate}
                onChange={(e) => setNewTournament({...newTournament, startDate: e.target.value})}
                required
              />
              <select
                value={newTournament.maxPlayers}
                onChange={(e) => setNewTournament({...newTournament, maxPlayers: parseInt(e.target.value)})}
              >
                <option value={8}>8 Players</option>
                <option value={16}>16 Players</option>
                <option value={32}>32 Players</option>
              </select>
              <div className="form-buttons">
                <button type="submit" className="submit-btn">Create</button>
                <button type="button" onClick={() => setShowCreateForm(false)} className="cancel-btn">Cancel</button>
              </div>
            </form>
          )}

          {error && <div className="error-message">{error}</div>}

          {loading ? (
            <p>Loading tournaments...</p>
          ) : tournaments.length === 0 ? (
            <p>No tournaments yet. Create one to get started!</p>
          ) : (
            <div className="tournaments-grid">
              {tournaments.map(tournament => (
                <div 
                  key={tournament.id}
                  className={`tournament-card ${selectedTournament?.id === tournament.id ? 'active' : ''}`}
                  onClick={() => handleSelectTournament(tournament)}
                >
                  <h3>{tournament.name}</h3>
                  <p className="status">{tournament.status}</p>
                  <p className="players">{tournament.participantCount} / {tournament.maxPlayers} Players</p>
                  <p className="admin">Admin: {tournament.adminName}</p>
                </div>
              ))}
            </div>
          )}
        </div>

        {selectedTournament && (
          <div className="matches-section">
            <div className="section-header">
              <h2>Matches - {selectedTournament.name}</h2>
            </div>

            {matches.length === 0 ? (
              <p>No matches in this tournament yet.</p>
            ) : (
              <div className="matches-list">
                {matches.map(match => (
                  <div key={match.id} className="match-card">
                    <h4>Match {match.id.substring(0, 8)}</h4>
                    <p>Format: {match.matchFormat}</p>
                    <p>Status: {match.status}</p>
                    <p>Participants: {match.participantsCount}/2</p>
                    {match.participantsCount < 2 && (
                      <button className="join-btn">Join Match</button>
                    )}
                  </div>
                ))}
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  )
}
