# Live Workshop Assistant

## Overview
The Live Workshop Assistant is a hands-free, voice-activated AI assistant designed specifically for automotive workshops. It allows mechanics to get diagnostic information, torque specs, and technical support while keeping their hands on the vehicle.

## Features
- **Voice-Activated**: Completely hands-free operation using microphone input
- **Real-time Transcription**: See what you're saying and the assistant's responses in real-time
- **Automotive Knowledge**: Specialized in:
  - Diagnostic procedures
  - Torque specifications
  - Parts information
  - Troubleshooting steps
  - Job card dictation

## Integration Points

### 1. Session Detail Page
- **Location**: Diagnostic session workflow pages
- **Access**: Floating microphone button (bottom-right corner)
- **Use Case**: Get live assistance while performing diagnostic procedures
- **Context**: Assistant has access to current session details

### 2. Job Card Detail Page
- **Location**: Active job card pages
- **Access**: Floating microphone button (bottom-right corner)
- **Use Case**: Get specs and guidance while performing repair work
- **Context**: Assistant knows the current job context

### 3. Dashboard
- **Location**: Main dashboard quick actions
- **Access**: "Live Assistant" quick action button
- **Use Case**: Quick access to diagnostic assistance from anywhere
- **Context**: General workshop assistance

## Configuration

### Gemini API Key
The Live Workshop Assistant uses Google's Gemini AI with native audio capabilities.

**Current Implementation**: 
- API Key is stored in environment files (`environment.ts` and `environment.prod.ts`)
- Key: `geminiApiKey: 'AIzaSyByYekd4QDnh4YhJRNhDvCGvXYmMtAgRzs'`

**Security Notes**:
- Environment files are git-ignored to prevent API key exposure
- Template file available at: `src/environments/environment.example.ts`
- For production, consider moving to backend API endpoint (FastAPI + LangGraph)

### Future Backend Integration
The component is designed to easily switch from direct Gemini API calls to a backend endpoint:

1. Create FastAPI endpoint at `/api/live-assistant`
2. Update `startSession()` method in `live-workshop-assistant.component.ts`
3. Replace Gemini SDK calls with HTTP requests to your API
4. Backend can use LangGraph for more complex workflows

## Usage

### For Mechanics
1. Click the microphone button (floating or quick action)
2. Wait for "Listening for Workshop Commands..." message
3. Speak naturally about your diagnostic needs
4. Assistant will respond with audio and text
5. Continue conversation as needed
6. Click "End Session" when finished

### Example Interactions
- "Check the oil pressure sensor on this Camry"
- "What's the torque spec for 2019 F-150 lug nuts?"
- "Diagnose P0420 code on Honda Civic"
- "Brake fluid type for BMW 3 Series"
- "Create job card note: replaced front brake pads"

## Technical Implementation

### Component Structure
```
src/app/components/live-workshop-assistant/
├── live-workshop-assistant.component.ts    # Main component logic
├── live-workshop-assistant.component.html  # Modal UI
└── live-workshop-assistant.component.css   # Animations & styling
```

### Current Features
- ✅ Microphone access and audio capture
- ✅ Real-time audio level detection
- ✅ Simulated AI responses (for demo)
- ✅ Beautiful modal UI with animations
- ✅ Floating action button integration
- ✅ State management with Angular signals

### Planned Features (When Gemini API is connected)
- [ ] Real-time audio streaming to Gemini
- [ ] Native audio responses from Gemini
- [ ] Live transcription (both user and AI)
- [ ] Context awareness (current vehicle, session, job card)
- [ ] Workshop-specific knowledge base
- [ ] Job card dictation and note-taking
- [ ] Parts lookup and inventory integration

## API Integration

### Using Google Gemini Live API
```typescript
import { GoogleGenAI, Modality } from '@google/genai';
import { environment } from '../../../environments/environment';

const ai = new GoogleGenAI({ apiKey: environment.geminiApiKey });

const session = await ai.live.connect({
  model: 'gemini-2.5-flash-native-audio-preview-09-2025',
  config: {
    responseModalities: [Modality.AUDIO],
    systemInstruction: 'You are Gixat Live, a hands-free workshop assistant...',
  }
});
```

### Audio Processing
- **Input**: 16kHz PCM audio from microphone
- **Output**: 24kHz PCM audio for playback
- **Format**: Int16 PCM, mono channel
- **Encoding**: Base64 for API transmission

## Browser Requirements
- Chrome/Edge: Full support
- Safari: Requires user permission for microphone
- Firefox: Full support

## Permissions Required
- **Microphone Access**: Required for voice input
- User will be prompted on first use
- Permission is remembered per domain

## Troubleshooting

### "Connection Failed - Check Permissions"
- Browser is blocking microphone access
- Click lock icon in address bar → Allow microphone
- Refresh page and try again

### No Audio Response
- Check system audio output
- Ensure browser tab is not muted
- Check volume levels

### Slow Response
- Check internet connection
- API rate limits may apply
- Consider backend caching for common queries

## Development Notes

### Simulated Mode
Currently, the component runs in simulated mode with pre-programmed responses. To activate real Gemini API:

1. Uncomment Gemini API code in `startSession()`
2. Install package: `npm install @google/genai`
3. Update environment.ts with valid API key
4. Remove `simulateConnection()` and `startSimulatedResponses()` calls

### Testing
```bash
# Build and test
npm run build
npm start

# Navigate to any of:
# - Session detail page
# - Job card detail page  
# - Dashboard

# Click microphone button to open Live Assistant
```

## Security Considerations

1. **API Key Protection**
   - Never commit API keys to git
   - Use environment variables
   - Rotate keys regularly
   - Consider backend proxy for production

2. **Audio Privacy**
   - Audio is processed in real-time
   - No permanent audio storage
   - Transcripts are temporary
   - Clear on session end

3. **Production Recommendations**
   - Move API calls to backend
   - Implement rate limiting
   - Add authentication checks
   - Log usage for auditing

## Support
For issues or questions about the Live Workshop Assistant, contact the Gixat development team.
