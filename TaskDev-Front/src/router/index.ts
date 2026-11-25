import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    { path: '/login', component: () => import('@/views/LoginView.vue'), meta: { public: true } },
    { path: '/register', component: () => import('@/views/RegisterView.vue'), meta: { public: true } },
    { path: '/', component: () => import('@/views/OverviewView.vue'), meta: { requiresAuth: true } },
    { path: '/spaces/:id', component: () => import('@/views/SpaceView.vue'), meta: { requiresAuth: true } },
    { path: '/settings', component: () => import('@/views/SettingsView.vue'), meta: { requiresAuth: true } },
    { path: '/admin', component: () => import('@/views/AdminView.vue'), meta: { requiresAuth: true, requiresAdmin: true } },
    { path: '/:pathMatch(.*)*', component: () => import('@/views/NotFoundView.vue') },
  ],
})

router.beforeEach(async (to) => {
  const authStore = useAuthStore()

  if (!authStore.initialized) {
    await authStore.initialize()
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    return '/login'
  }

  if (to.meta.public && authStore.isAuthenticated) {
    return '/'
  }

  if (to.meta.requiresAdmin && !authStore.isOperator) {
    return '/'
  }
})

export default router
