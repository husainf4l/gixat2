import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Get token from localStorage
  const token = localStorage.getItem('auth_token');

  // Clone the request and add authorization header if token exists
  const authReq = token
    ? req.clone({
        setHeaders: {
          authorization: `Bearer ${token}`
        }
      })
    : req;

  return next(authReq);
};