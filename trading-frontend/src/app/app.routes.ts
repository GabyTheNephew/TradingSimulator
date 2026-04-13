import { Routes } from '@angular/router';
import { RegisterComponent } from './components/register/register.component';
import { LoginComponent } from './components/login/login.component';
import { DashboardComponent } from './components/dashboard/dashboard.component';
import { authGuard } from './guards/auth-guard';
import { PortfolioComponent } from './components/portfolio/portfolio.component';
import { TutorialComponent } from './components/tutorial/tutorial.component';

export const routes: Routes = [
    {path: 'register', component: RegisterComponent},
    {path: 'login', component: LoginComponent},
    {path:'dashboard', component: DashboardComponent, canActivate: [authGuard]},
    { path: 'portfolio', component: PortfolioComponent, canActivate: [authGuard] },
    { path: 'tutorial', component: TutorialComponent, canActivate: [authGuard] },
    {path:'', redirectTo:'login', pathMatch:'full'}
];
