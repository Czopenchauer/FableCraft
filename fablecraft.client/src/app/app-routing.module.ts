import {NgModule} from '@angular/core';
import {RouterModule, Routes} from '@angular/router';
import { HomeComponent } from './features/home/components/home/home.component';
import { AdventureListComponent } from './features/adventures/components/adventure-list/adventure-list.component';
import { AdventureCreateComponent } from './features/adventures/components/adventure-create/adventure-create.component';

const routes: Routes = [
  { path: '', component: HomeComponent },
  { path: 'adventures', component: AdventureListComponent },
  { path: 'adventures/create', component: AdventureCreateComponent },
  { path: '**', redirectTo: '' }
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {
}
