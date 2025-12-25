declare const window: any;

export const environment = {
  production: false,
  get apiUrl() {
    return window.__env?.apiUrl || 'http://localhost:8002';
  },
  googleClientId: '452012051448-9n3pmrnaccuaikme75dg1vqo6la8tiem.apps.googleusercontent.com',
  geminiApiKey: 'AIzaSyByYekd4QDnh4YhJRNhDvCGvXYmMtAgRzs'
};
