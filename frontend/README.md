# DartMaster Frontend

React TypeScript frontend for DartMaster tournament management system.

## Features

- User authentication (Login/Register)
- Tournament management dashboard
- Match viewing and participation
- Real-time score tracking
- Responsive UI

## Tech Stack

- **React 18** - UI library
- **TypeScript** - Type safety
- **Vite** - Build tool
- **React Router DOM** - Client-side routing
- **Axios** - HTTP client

## Project Structure

```
src/
├── components/          # Reusable components
│   └── ProtectedRoute.tsx
├── pages/              # Page components
│   ├── LoginPage.tsx
│   ├── RegisterPage.tsx
│   └── DashboardPage.tsx
├── services/           # API integration
│   └── api.ts
├── styles/             # CSS files
│   ├── Auth.css
│   └── Dashboard.css
├── App.tsx            # Main app router
├── main.tsx           # Entry point
└── index.css          # Global styles
```

## Getting Started

### Prerequisites

- Node.js 16+
- npm or yarn

### Installation

```bash
cd frontend
npm install
```

### Development

```bash
npm run dev
```

The app will be available at `http://localhost:5173`

### Build

```bash
npm run build
```

### Lint

```bash
npm run lint
```

## API Integration

The frontend connects to the backend API at `http://localhost:5146/api`

### Available Endpoints

- POST `/users/login` - User login
- POST `/users/register` - User registration
- GET `/tournaments` - Get all tournaments
- POST `/tournaments` - Create tournament
- GET `/matches?tournamentId={id}` - Get tournament matches

## Environment Setup

The Vite proxy is configured in `vite.config.ts` to forward API calls to the backend server.

## Features

### Authentication
- User registration with validation
- JWT token-based login
- Secure token storage
- Protected routes

### Tournaments
- View all tournaments
- Create new tournaments
- Tournament status tracking
- Player count display

### Matches
- View matches for a tournament
- Join available matches
- Match status display
- Participant management

## License

MIT
