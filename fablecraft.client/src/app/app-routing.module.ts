import {NgModule} from '@angular/core';
import {RouterModule, Routes} from '@angular/router';
import {HomeComponent} from './features/home/components/home/home.component';
import {AdventureListComponent} from './features/adventures/components/adventure-list/adventure-list.component';
import {AdventureCreateComponent} from './features/adventures/components/adventure-create/adventure-create.component';
import {AdventureStatusComponent} from './features/adventures/components/adventure-status/adventure-status.component';
import {GamePanelComponent} from './features/adventures/components/game-panel/game-panel.component';
import {WorldbookListComponent} from './features/adventures/components/worldbook-list/worldbook-list.component';
import {WorldbookFormComponent} from './features/adventures/components/worldbook-form/worldbook-form.component';

const routes: Routes = [
  {path: '', component: HomeComponent},
  {path: 'adventures', component: AdventureListComponent},
  {path: 'adventures/create', component: AdventureCreateComponent},
  {path: 'adventures/status/:id', component: AdventureStatusComponent},
  {path: 'adventures/play/:id', component: GamePanelComponent},
  {path: 'worldbooks', component: WorldbookListComponent},
  {path: 'worldbooks/create', component: WorldbookFormComponent},
  {path: 'worldbooks/edit/:id', component: WorldbookFormComponent},
  {path: '**', redirectTo: ''}
];

@NgModule({
  imports: [RouterModule.forRoot(routes)],
  exports: [RouterModule]
})
export class AppRoutingModule {
}
