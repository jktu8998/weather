// ==================== НАСТРОЙКИ ====================
const API_BASE_URL = 'http://localhost:5150/api'; // замените на свой URL, если нужно

// ==================== РАБОТА С ТОКЕНАМИ ====================
function saveTokens(accessToken, refreshToken) {
    localStorage.setItem('accessToken', accessToken);
    localStorage.setItem('refreshToken', refreshToken);
}

function getAccessToken() {
    return localStorage.getItem('accessToken');
}

function getRefreshToken() {
    return localStorage.getItem('refreshToken');
}

function clearTokens() {
    localStorage.removeItem('accessToken');
    localStorage.removeItem('refreshToken');
}
async function fetchWithAuth(url, options = {}) {
    let accessToken = getAccessToken();

    // Первый запрос с текущим токеном
    let response = await fetch(url, {
        ...options,
        headers: {
            ...options.headers,
            'Authorization': `Bearer ${accessToken}`,
            'Content-Type': 'application/json'
        }
    });

    // Если токен истёк (401)
    if (response.status === 401) {
        const refreshToken = getRefreshToken();
        if (!refreshToken) {
            clearTokens();
            showAuthBlock();
            throw new Error('Сессия истекла. Пожалуйста, войдите снова.');
        }

        // Пытаемся обновить токен
        try {
            const refreshResponse = await fetch(`${API_BASE_URL}/auth/refresh`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ refreshToken })
            });

            if (!refreshResponse.ok) {
                // Если refresh не удался (например, токен тоже истёк)
                clearTokens();
                showAuthBlock();
                throw new Error('Не удалось обновить сессию. Войдите заново.');
            }

            const tokens = await refreshResponse.json();
            saveTokens(tokens.accessToken, tokens.refreshToken);

            // Повторяем исходный запрос с новым токеном
            response = await fetch(url, {
                ...options,
                headers: {
                    ...options.headers,
                    'Authorization': `Bearer ${tokens.accessToken}`,
                    'Content-Type': 'application/json'
                }
            });
        } catch (error) {
            clearTokens();
            showAuthBlock();
            throw error;
        }
    }

    return handleResponse(response);
}
// ==================== AUTH API ====================

async function register(email, password) {
    const response = await fetch(`${API_BASE_URL}/auth/register`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
    });
    return handleResponse(response); // возвращает { message: "Registration successful" }
}

async function login(email, password) {
    const response = await fetch(`${API_BASE_URL}/auth/login`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password })
    });
    const data = await handleResponse(response); // TokenResponse
    saveTokens(data.accessToken, data.refreshToken);
    return data;
}

async function changePassword(currentPassword, newPassword) {
    return fetchWithAuth(`${API_BASE_URL}/auth/update`, {
        method: 'POST',
        body: JSON.stringify({ currentPassword, newPassword })
    });
}

async function deleteAccount(password) {
    return fetchWithAuth(`${API_BASE_URL}/auth/delete`, {
        method: 'POST',
        body: JSON.stringify({ password })
    });
}
// ==================== ПЕРЕКЛЮЧЕНИЕ ЭКРАНОВ ====================
function showAuthBlock() {
    document.getElementById('auth-block').style.display = 'block';
    document.getElementById('weather-block').style.display = 'none';
}

function showWeatherBlock() {
    document.getElementById('auth-block').style.display = 'none';
    document.getElementById('weather-block').style.display = 'block';
}

// ==================== ОБРАБОТКА ОШИБОК ====================
async function handleResponse(response) {
    if (!response.ok) {
        let errorMessage = `Ошибка ${response.status}`;
        try {
            const errorData = await response.json();
            // предполагаем, что ошибка приходит в поле "error" или "message"
            errorMessage = errorData.error || errorData.message || errorMessage;
        } catch (e) {
            // если ответ не JSON
        }
        throw new Error(errorMessage);
    }
    return response.json();
}
// ==================== WEATHER API ====================

async function getWeather(city) {
    return fetchWithAuth(`${API_BASE_URL}/weather/${encodeURIComponent(city)}`);
}

async function compareCities(city1, city2) {
    return fetchWithAuth(`${API_BASE_URL}/weather/compare`, {
        method: 'POST',
        body: JSON.stringify({ cities: [city1, city2] })
    });
}
function renderWeather(data, containerId) {
    const container = document.getElementById(containerId);
    container.innerHTML = ''; // очищаем

    const card = document.createElement('div');
    card.className = 'weather-card';

    // Город
    const cityDiv = document.createElement('div');
    cityDiv.className = 'weather-city';
    cityDiv.textContent = data.city;
    card.appendChild(cityDiv);

    // Основные показатели
    const mainDiv = document.createElement('div');
    mainDiv.className = 'weather-main';
    mainDiv.innerHTML = `
        <span class="weather-temp">${data.average.temperatureC.toFixed(1)}°C</span>
        <span class="weather-condition">${data.average.condition}</span>
        <span class="weather-wind">Ветер: ${data.average.windSpeedKph.toFixed(1)} км/ч</span>
    `;
    card.appendChild(mainDiv);

    // Кнопка деталей
    const detailsBtn = document.createElement('button');
    detailsBtn.className = 'details-btn';
    detailsBtn.textContent = 'Показать детали';
    card.appendChild(detailsBtn);

    // Таблица деталей (изначально скрыта)
    const table = document.createElement('table');
    table.className = 'details-table';
    table.style.display = 'none'; // <-- скрываем сразу
    table.innerHTML = `
        <thead>
            <tr><th>Источник</th><th>Температура (°C)</th><th>Ветер (км/ч)</th><th>Условия</th></tr>
        </thead>
        <tbody>
            ${data.details.map(d => `
                <tr>
                    <td>${d.source}</td>
                    <td>${d.temperatureC.toFixed(1)}</td>
                    <td>${d.windSpeedKph.toFixed(1)}</td>
                    <td>${d.condition}</td>
                </tr>
            `).join('')}
        </tbody>
    `;
    card.appendChild(table);

    // Обработчик кнопки
    detailsBtn.addEventListener('click', () => {
        if (table.style.display === 'none') {
            table.style.display = 'table';
            detailsBtn.textContent = 'Скрыть детали';
        } else {
            table.style.display = 'none';
            detailsBtn.textContent = 'Показать детали';
        }
    });

    container.appendChild(card);
}
function renderComparison(data, containerId) {
    const container = document.getElementById(containerId);
    container.innerHTML = '';

    // Карточка первого города
    const city1Card = document.createElement('div');
    city1Card.className = 'compare-card';
    fillCityCard(city1Card, data.city1);
    container.appendChild(city1Card);

    // Карточка второго города
    const city2Card = document.createElement('div');
    city2Card.className = 'compare-card';
    fillCityCard(city2Card, data.city2);
    container.appendChild(city2Card);

    // Блок с разницей
    const summaryDiv = document.createElement('div');
    summaryDiv.className = 'comparison-summary';
    const comp = data.comparison;
    summaryDiv.innerHTML = `
        <h4>Сравнение</h4>
        <p>🌡️ Разница температур: ${comp.temperatureDifference.toFixed(1)}°C (теплее в <span class="winner">${comp.warmerCity}</span>)</p>
        <p>💨 Разница ветра: ${comp.windDifference.toFixed(1)} км/ч (менее ветрено в <span class="winner">${comp.lessWindyCity}</span>)</p>
        <p>📝 ${comp.summary}</p>
    `;
    container.appendChild(summaryDiv);
}

// Вспомогательная функция для заполнения карточки города  
function fillCityCard(card, cityData) {
    // Город
    const cityDiv = document.createElement('div');
    cityDiv.className = 'weather-city';
    cityDiv.textContent = cityData.city;
    card.appendChild(cityDiv);

    // Основные показатели
    const mainDiv = document.createElement('div');
    mainDiv.className = 'weather-main';
    mainDiv.innerHTML = `
        <span class="weather-temp">${cityData.average.temperatureC.toFixed(1)}°C</span>
        <span class="weather-condition">${cityData.average.condition}</span>
        <span class="weather-wind">Ветер: ${cityData.average.windSpeedKph.toFixed(1)} км/ч</span>
    `;
    card.appendChild(mainDiv);

    // Кнопка деталей
    const detailsBtn = document.createElement('button');
    detailsBtn.className = 'details-btn';
    detailsBtn.textContent = 'Показать детали';
    card.appendChild(detailsBtn);

    // Таблица деталей (изначально скрыта)
    const table = document.createElement('table');
    table.className = 'details-table';
    table.style.display = 'none'; // <-- скрываем сразу
    table.innerHTML = `
        <thead>
            <tr><th>Источник</th><th>Температура (°C)</th><th>Ветер (км/ч)</th><th>Условия</th></tr>
        </thead>
        <tbody>
            ${cityData.details.map(d => `
                <tr>
                    <td>${d.source}</td>
                    <td>${d.temperatureC.toFixed(1)}</td>
                    <td>${d.windSpeedKph.toFixed(1)}</td>
                    <td>${d.condition}</td>
                </tr>
            `).join('')}
        </tbody>
    `;
    card.appendChild(table);

    detailsBtn.addEventListener('click', () => {
        if (table.style.display === 'none') {
            table.style.display = 'table';
            detailsBtn.textContent = 'Скрыть детали';
        } else {
            table.style.display = 'none';
            detailsBtn.textContent = 'Показать детали';
        }
    });
}
// ==================== DOM ЭЛЕМЕНТЫ ====================
const authBlock = document.getElementById('auth-block');
const weatherBlock = document.getElementById('weather-block');

// Формы
const loginForm = document.getElementById('login-form');
const registerForm = document.getElementById('register-form');

// Кнопки переключения
document.getElementById('show-register').addEventListener('click', (e) => {
    e.preventDefault();
    loginForm.style.display = 'none';
    registerForm.style.display = 'block';
});

document.getElementById('show-login').addEventListener('click', (e) => {
    e.preventDefault();
    registerForm.style.display = 'none';
    loginForm.style.display = 'block';
});

// Регистрация
document.getElementById('register-btn').addEventListener('click', async () => {
    const email = document.getElementById('reg-email').value;
    const password = document.getElementById('reg-password').value;
    try {
        await register(email, password);
        alert('Регистрация успешна! Теперь войдите.');
        // Переключаем на форму входа
        registerForm.style.display = 'none';
        loginForm.style.display = 'block';
        // Очищаем поля
        document.getElementById('reg-email').value = '';
        document.getElementById('reg-password').value = '';
    } catch (error) {
        alert(error.message);
    }
});

// Логин
document.getElementById('login-btn').addEventListener('click', async () => {
    const email = document.getElementById('login-email').value;
    const password = document.getElementById('login-password').value;
    try {
        await login(email, password);
        // Очищаем поля
        document.getElementById('login-email').value = '';
        document.getElementById('login-password').value = '';
        showWeatherBlock();
    } catch (error) {
        alert(error.message);
    }
});
// Смена пароля
document.getElementById('change-password-btn').addEventListener('click', async () => {
    const current = document.getElementById('current-password').value;
    const newPass = document.getElementById('new-password').value;
    try {
        await changePassword(current, newPass);
        alert('Пароль успешно изменён');
        document.getElementById('current-password').value = '';
        document.getElementById('new-password').value = '';
    } catch (error) {
        alert(error.message);
    }
});

// Удаление аккаунта
document.getElementById('delete-account-btn').addEventListener('click', async () => {
    const password = document.getElementById('delete-password').value;
    if (!confirm('Вы уверены? Это действие необратимо.')) return;
    try {
        await deleteAccount(password);
        alert('Аккаунт удалён');
        clearTokens();
        showAuthBlock();
    } catch (error) {
        alert(error.message);
    }
});

// Выход
document.getElementById('logout-btn').addEventListener('click', () => {
    clearTokens();
    showAuthBlock();
});

// Получить погоду для одного города
document.getElementById('get-weather-btn').addEventListener('click', async () => {
    const city = document.getElementById('city-single').value.trim();
    if (!city) {
        alert('Введите город');
        return;
    }
    try {
        const data = await getWeather(city);
        renderWeather(data, 'weather-result');
    } catch (error) {
        alert(error.message);
    }
});

// Сравнить два города
document.getElementById('compare-btn').addEventListener('click', async () => {
    const city1 = document.getElementById('city1').value.trim();
    const city2 = document.getElementById('city2').value.trim();
    if (!city1 || !city2) {
        alert('Введите оба города');
        return;
    }
    try {
        const data = await compareCities(city1, city2);
        renderComparison(data, 'compare-result');
    } catch (error) {
        alert(error.message);
    }
});

// При загрузке страницы проверяем, авторизован ли пользователь
if (getAccessToken()) {
    showWeatherBlock();
} else {
    showAuthBlock();
}