import { HttpInterceptorFn } from '@angular/common/http';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  // Luăm token-ul salvat în localStorage la momentul login-ului
  const token = localStorage.getItem('jwtToken'); 

  if (token) {
    // Dacă avem token, clonăm cererea și adăugăm header-ul de Authorization
    const clonedReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
    return next(clonedReq);
  }

  // Dacă nu avem token, lăsăm cererea să treacă nemodificată
  return next(req);
};