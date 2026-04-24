const apiBases = [
    "http://localhost:5055/api",
    "http://127.0.0.1:5055/api"
];

const state = {
    bootstrap: null,
    user: null,
    products: [],
    tables: [],
    orders: [],
    cart: [],
    admin: {
        entity: "Products",
        payload: null,
        selectedId: 0
    },
    apiBase: apiBases[0]
};

const money = new Intl.NumberFormat("de-DE", {
    style: "currency",
    currency: "EUR"
});

document.addEventListener("DOMContentLoaded", async () => {
    bindStaticEvents();
    await loadBootstrap();
    restoreUser();
});

function bindStaticEvents() {
    on("loginButton", "click", login);
    on("bootstrapButton", "click", loadBootstrap);
    on("refreshAllButton", "click", refreshData);
    on("logoutButton", "click", logout);
    on("clearCartButton", "click", clearCart);
    on("sendOrderButton", "click", sendOrder);
    on("closeOrderButton", "click", closeOrder);
    on("adminEntitySelect", "change", event => {
        state.admin.entity = event.target.value;
        loadAdminEntity();
    });
    on("newRecordButton", "click", clearAdminForm);
    on("saveRecordButton", "click", saveAdminRecord);
    on("deleteRecordButton", "click", deleteAdminRecord);
    on("tableSelect", "change", renderCart);
    on("paymentOrderSelect", "change", renderPaymentPanel);

    document.querySelectorAll(".nav-link").forEach(button => {
        button.addEventListener("click", () => switchView(button.dataset.view));
    });

    document.getElementById("rfidInput").addEventListener("keydown", event => {
        if (event.key === "Enter")
            login();
    });
}

async function loadBootstrap() {
    try {
        const payload = await api("/bootstrap");
        state.bootstrap = payload;
        state.products = payload.products || [];
        state.tables = payload.tables || [];
        state.orders = payload.orders || [];
        renderDemoUsers();
        renderGlobal();
        setMessage("loginMessage", payload.databaseReady ? "Backend and database are ready." : payload.databaseError, !payload.databaseReady);
    } catch (error) {
        setMessage("loginMessage", "Backend API is not reachable. Start the WinForms app first, then use Refresh Data.", true);
    }
}

async function refreshData() {
    try {
        const [products, tables, orders] = await Promise.all([
            api("/products"),
            api("/tables"),
            api("/orders")
        ]);
        state.products = products;
        state.tables = tables;
        state.orders = orders;
        renderGlobal();
        if (state.user && isAdmin())
            await loadAdminEntity();
    } catch (error) {
        alert(error.message);
    }
}

function restoreUser() {
    const raw = localStorage.getItem("rms-user");
    if (!raw)
        return;

    try {
        state.user = JSON.parse(raw);
        enterApp();
    } catch {
        localStorage.removeItem("rms-user");
    }
}

async function login() {
    const rfid = document.getElementById("rfidInput").value.trim();
    if (!rfid) {
        setMessage("loginMessage", "Write the RFID code first.", true);
        return;
    }

    try {
        const user = await api("/login", {
            method: "POST",
            body: JSON.stringify({ rfid })
        });
        state.user = user;
        localStorage.setItem("rms-user", JSON.stringify(user));
        setMessage("loginMessage", `Welcome ${user.Name}.`, false);
        enterApp();
    } catch (error) {
        setMessage("loginMessage", error.message, true);
    }
}

function enterApp() {
    document.getElementById("loginScreen").classList.add("hidden");
    document.getElementById("appRoot").classList.remove("hidden");
    document.getElementById("userName").textContent = state.user.Name;
    document.getElementById("userRole").textContent = state.user.Role;
    document.querySelectorAll(".admin-only").forEach(item => {
        item.classList.toggle("hidden", !isAdmin());
    });
    if (!isAdmin() && document.querySelector(".nav-link.active").dataset.view === "admin")
        switchView("pos");
    renderGlobal();
    refreshData();
    if (isAdmin())
        loadAdminEntity();
}

function logout() {
    localStorage.removeItem("rms-user");
    state.user = null;
    state.cart = [];
    document.getElementById("appRoot").classList.add("hidden");
    document.getElementById("loginScreen").classList.remove("hidden");
}

function switchView(view) {
    if (view === "admin" && !isAdmin())
        return;

    document.querySelectorAll(".nav-link").forEach(button => {
        button.classList.toggle("active", button.dataset.view === view);
    });
    document.querySelectorAll(".view").forEach(section => {
        section.classList.toggle("active", section.id === `view-${view}`);
    });

    const titles = {
        dashboard: ["Live Operations", "Restaurant Overview"],
        pos: ["Waiter Flow", "POS and Payments"],
        kitchen: ["Production", "Kitchen Monitor"],
        admin: ["Database Admin", "Manage Products, Tables, Users"]
    };

    document.getElementById("viewEyebrow").textContent = titles[view][0];
    document.getElementById("viewTitle").textContent = titles[view][1];
}

function renderGlobal() {
    renderStats();
    renderTables();
    renderProducts();
    renderOrders();
    renderCart();
    renderPaymentPanel();
}

function renderDemoUsers() {
    const host = document.getElementById("demoUsers");
    if (!state.bootstrap || !state.bootstrap.demoUsers)
        return;

    host.innerHTML = state.bootstrap.demoUsers.map(user => `
        <button class="demo-pill" type="button" data-rfid="${user.rfid}">
            <span>${user.role}</span>
            <strong>${user.rfid}</strong>
        </button>
    `).join("");

    host.querySelectorAll(".demo-pill").forEach(button => {
        button.addEventListener("click", () => {
            document.getElementById("rfidInput").value = button.dataset.rfid;
        });
    });
}

function renderStats() {
    text("statProducts", state.products.length);
    text("statTables", state.tables.length);
    text("statOrders", state.orders.length);
    text("statTotal", money.format(state.orders.reduce((sum, order) => sum + Number(order.Total || 0), 0)));
}

function renderTables() {
    const tableMarkup = state.tables.length
        ? state.tables.map(table => `
            <button class="table-card ${statusClass(table.Status)}" data-table-id="${table.Id}">
                ${escapeHtml(table.Name)}
                <span>${escapeHtml(table.Status)}</span>
            </button>
        `).join("")
        : `<div class="empty-state">No tables found.</div>`;

    document.getElementById("overviewTables").innerHTML = tableMarkup;

    const select = document.getElementById("tableSelect");
    const currentValue = select.value;
    select.innerHTML = `<option value="">Choose table</option>` + state.tables.map(table => `
        <option value="${table.Id}">${escapeHtml(table.Name)} - ${escapeHtml(table.HallName || "Hall")}</option>
    `).join("");
    select.value = currentValue;

    document.querySelectorAll("[data-table-id]").forEach(button => {
        button.addEventListener("click", () => {
            select.value = button.dataset.tableId;
            renderCart();
            switchView("pos");
        });
    });
}

function renderProducts() {
    const menuGrid = document.getElementById("menuGrid");
    if (!state.products.length) {
        menuGrid.innerHTML = `<div class="empty-state">No products available.</div>`;
        return;
    }

    menuGrid.innerHTML = state.products.map(product => `
        <button class="menu-item" data-product-id="${product.Id}">
            <span>${iconFor(product.Name, product.CategoryName)}</span>
            <strong>${escapeHtml(product.Name)}</strong>
            <small>${escapeHtml(product.CategoryName || "General")}</small>
            <em>${money.format(product.PriceWithVat)}</em>
        </button>
    `).join("");

    menuGrid.querySelectorAll(".menu-item").forEach(button => {
        button.addEventListener("click", () => addToCart(Number(button.dataset.productId)));
    });
}

function renderOrders() {
    const markup = state.orders.length
        ? state.orders.map(order => `
            <article class="kitchen-card ${statusClass(order.Status)}">
                <div class="card-top">
                    <div>
                        <strong>Order #${order.Id}</strong>
                        <span>${escapeHtml(order.TableName)} · ${escapeHtml(order.WaiterName)}</span>
                    </div>
                    <span class="status ${statusClass(order.Status)}">${escapeHtml(order.Status)}</span>
                </div>
                <p>${order.Items.map(item => `${item.Quantity}x ${escapeHtml(item.ProductName)}`).join(", ")}</p>
                <small>${order.Items.map(item => item.Comment).filter(Boolean).join(" | ") || "No comment"}</small>
                <div class="kitchen-actions">
                    <button data-order-status="Pending" data-order-id="${order.Id}">Pending</button>
                    <button data-order-status="Preparing" data-order-id="${order.Id}">Preparing</button>
                    <button data-order-status="Ready" data-order-id="${order.Id}">Ready</button>
                </div>
            </article>
        `).join("")
        : `<div class="empty-state">No active orders.</div>`;

    document.getElementById("overviewOrders").innerHTML = markup;
    document.getElementById("kitchenOrders").innerHTML = markup;

    document.querySelectorAll("[data-order-status]").forEach(button => {
        button.addEventListener("click", () => updateOrderStatus(Number(button.dataset.orderId), button.dataset.orderStatus));
    });

    const paymentSelect = document.getElementById("paymentOrderSelect");
    const current = paymentSelect.value;
    paymentSelect.innerHTML = `<option value="">Choose order</option>` + state.orders.map(order => `
        <option value="${order.Id}">#${order.Id} - ${escapeHtml(order.TableName)} - ${money.format(order.Total)}</option>
    `).join("");
    paymentSelect.value = current;
}

function renderCart() {
    const selectedTableId = Number(document.getElementById("tableSelect").value || 0);
    const selectedTable = state.tables.find(item => item.Id === selectedTableId);
    text("selectedTableLabel", selectedTable ? `${selectedTable.Name} selected` : "Choose a table");
    text("cartTotal", money.format(cartTotal()));

    const cartList = document.getElementById("cartList");
    if (!state.cart.length) {
        cartList.innerHTML = `<div class="empty-state">Click products to build the order.</div>`;
        return;
    }

    cartList.innerHTML = state.cart.map(item => `
        <div class="order-row">
            <div>
                <strong>${item.quantity}x ${escapeHtml(item.name)}</strong>
                <small>${money.format(item.priceWithVat)} each</small>
            </div>
            <div class="row-actions">
                <button class="tiny-button" data-cart-action="minus" data-product-id="${item.productId}">-</button>
                <button class="tiny-button" data-cart-action="plus" data-product-id="${item.productId}">+</button>
            </div>
        </div>
    `).join("");

    cartList.querySelectorAll("[data-cart-action]").forEach(button => {
        button.addEventListener("click", () => adjustCart(Number(button.dataset.productId), button.dataset.cartAction === "plus" ? 1 : -1));
    });
}

function renderPaymentPanel() {
    const orderId = Number(document.getElementById("paymentOrderSelect").value || 0);
    const order = state.orders.find(item => item.Id === orderId);
    text("paymentTotal", money.format(order ? order.Total : 0));
}

function addToCart(productId) {
    const product = state.products.find(item => item.Id === productId);
    if (!product)
        return;
    const existing = state.cart.find(item => item.productId === productId);
    if (existing) {
        existing.quantity += 1;
    } else {
        state.cart.push({
            productId: product.Id,
            name: product.Name,
            quantity: 1,
            priceWithVat: Number(product.PriceWithVat)
        });
    }
    renderCart();
}

function adjustCart(productId, delta) {
    const item = state.cart.find(entry => entry.productId === productId);
    if (!item)
        return;
    item.quantity += delta;
    if (item.quantity <= 0)
        state.cart = state.cart.filter(entry => entry.productId !== productId);
    renderCart();
}

function clearCart() {
    state.cart = [];
    renderCart();
}

async function sendOrder() {
    if (!state.user) {
        alert("Login first.");
        return;
    }
    const tableId = Number(document.getElementById("tableSelect").value || 0);
    if (!tableId) {
        alert("Choose a table.");
        return;
    }
    if (!state.cart.length) {
        alert("Add items to the cart.");
        return;
    }

    await api("/orders", {
        method: "POST",
        body: JSON.stringify({
            tableId,
            waiterId: state.user.Id,
            items: state.cart.map(item => ({
                productId: item.productId,
                quantity: item.quantity,
                comment: ""
            }))
        })
    });

    clearCart();
    await refreshData();
    alert("Order saved in database and sent to kitchen.");
}

async function updateOrderStatus(orderId, status) {
    if (!isAdmin()) {
        alert("Only admin can update kitchen status.");
        return;
    }
    await api(`/orders/${orderId}/status`, {
        method: "PUT",
        body: JSON.stringify({ status })
    });
    await refreshData();
}

async function closeOrder() {
    if (!state.user) {
        alert("Login first.");
        return;
    }

    const orderId = Number(document.getElementById("paymentOrderSelect").value || 0);
    if (!orderId) {
        alert("Choose an active order.");
        return;
    }

    const order = state.orders.find(item => item.Id === orderId);
    if (!order)
        return;

    const paymentMethod = document.getElementById("paymentMethod").value;
    await api(`/orders/${orderId}/close`, {
        method: "POST",
        body: JSON.stringify({
            total: order.Total,
            paymentMethod
        })
    });

    await refreshData();
    alert("Order closed and payment stored in database.");
}

async function loadAdminEntity() {
    if (!isAdmin())
        return;

    state.admin.payload = await api(`/admin/${state.admin.entity}`);
    state.admin.selectedId = 0;
    renderAdminTable();
    renderAdminForm();
}

function renderAdminTable() {
    const payload = state.admin.payload;
    if (!payload) {
        document.getElementById("adminTableHead").innerHTML = "";
        document.getElementById("adminTableBody").innerHTML = `<tr><td>No data</td></tr>`;
        return;
    }

    document.getElementById("adminTableHead").innerHTML = `<tr>${payload.columns.map(column => `<th>${escapeHtml(column)}</th>`).join("")}</tr>`;
    document.getElementById("adminTableBody").innerHTML = payload.rows.map(row => `
        <tr data-admin-id="${row.Id || 0}">
            ${payload.columns.map(column => `<td>${escapeHtml(row[column])}</td>`).join("")}
        </tr>
    `).join("");

    document.querySelectorAll("[data-admin-id]").forEach(row => {
        row.addEventListener("click", () => {
            state.admin.selectedId = Number(row.dataset.adminId);
            renderAdminForm();
            document.querySelectorAll("[data-admin-id]").forEach(item => item.classList.remove("selected"));
            row.classList.add("selected");
        });
    });
}

function renderAdminForm() {
    const form = document.getElementById("adminForm");
    const payload = state.admin.payload;
    if (!payload) {
        form.innerHTML = "";
        return;
    }

    const selectedRow = payload.rows.find(row => Number(row.Id || 0) === state.admin.selectedId) || {};
    form.innerHTML = payload.columns.map(column => `
        <label class="field ${column === "Id" ? "hidden" : ""}">
            <span>${escapeHtml(column)}</span>
            <input name="${escapeHtml(column)}" value="${escapeAttribute(selectedRow[column])}" ${column === "Id" ? "readonly" : ""}>
        </label>
    `).join("");
}

function clearAdminForm() {
    state.admin.selectedId = 0;
    renderAdminForm();
}

async function saveAdminRecord() {
    const form = document.getElementById("adminForm");
    const values = {};
    form.querySelectorAll("input").forEach(input => {
        values[input.name] = input.value;
    });

    await api(`/admin/${state.admin.entity}`, {
        method: "POST",
        body: JSON.stringify(values)
    });

    await refreshData();
    await loadAdminEntity();
    alert("Record saved.");
}

async function deleteAdminRecord() {
    if (!state.admin.selectedId) {
        alert("Choose a record first.");
        return;
    }
    await api(`/admin/${state.admin.entity}/${state.admin.selectedId}`, {
        method: "DELETE"
    });
    await refreshData();
    await loadAdminEntity();
    alert("Record deleted.");
}

async function api(path, options = {}) {
    let lastError = null;

    for (let i = 0; i < apiBases.length; i++) {
        const base = i === 0 ? state.apiBase : apiBases[i];
        try {
            const response = await fetch(base + path, {
                headers: { "Content-Type": "application/json" },
                ...options
            });
            const data = await response.json();
            if (!response.ok || data.error)
                throw new Error(data.error || "Request failed.");

            state.apiBase = base;
            return data;
        } catch (error) {
            lastError = error;
        }
    }

    throw lastError || new Error("API unavailable.");
}

function isAdmin() {
    return state.user && String(state.user.Role).toLowerCase() === "admin";
}

function cartTotal() {
    return state.cart.reduce((sum, item) => sum + item.quantity * item.priceWithVat, 0);
}

function statusClass(status) {
    const value = String(status || "").toLowerCase();
    if (value === "ready")
        return "ready";
    if (value === "preparing" || value === "busy")
        return "preparing";
    return "pending";
}

function iconFor(name, category) {
    const value = `${name} ${category}`.toLowerCase();
    if (value.includes("pizza")) return "🍕";
    if (value.includes("coffee")) return "☕";
    if (value.includes("drink") || value.includes("water") || value.includes("cola")) return "🥤";
    if (value.includes("salad")) return "🥗";
    if (value.includes("pasta")) return "🍝";
    return "🍔";
}

function on(id, event, handler) {
    document.getElementById(id).addEventListener(event, handler);
}

function text(id, value) {
    document.getElementById(id).textContent = value;
}

function setMessage(id, message, error) {
    const node = document.getElementById(id);
    node.textContent = message || "";
    node.classList.toggle("error", !!error);
}

function escapeHtml(value) {
    return String(value ?? "")
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;")
        .replace(/"/g, "&quot;")
        .replace(/'/g, "&#039;");
}

function escapeAttribute(value) {
    return escapeHtml(value).replace(/\n/g, " ");
}
