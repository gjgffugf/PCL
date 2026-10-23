const CONFIG = {
  appName: "pcl-potato",
  slogan: "基于 PCL2 二次开发的 Minecraft 启动器",
  version: "3.0.0",
  versionCode: 500,
  size: "约 45 MB",
  updateDate: "2026-10-23",
  downloadUrl: "",
  sha256: "",
  releaseState: "soon",
  requireNetFramework: ".NET Framework 4.8"
};

const INTRO = {
  title: "什么是 pcl-potato",
  text: "pcl-potato 是基于 Plain Craft Launcher 2（PCL2，作者 龙腾猫跃）二次开发的 Minecraft 启动器。它在保留 PCL2 全部能力的基础上，恢复了 Minecraft 版本下载、取消了主题解锁限制，并把更新提示改为在线下发。",
  features: [
    { icon: "🎮", title: "恢复版本下载", desc: "Minecraft 正式版、快照版及各类加载器均可正常下载安装" },
    { icon: "🎨", title: "主题全解锁", desc: "15 个主题全部开放，无需赞助解锁即可自由切换" },
    { icon: "🔄", title: "在线更新提示", desc: "更新内容由服务器下发，四个源自动回退，一个挂了不影响" },
    { icon: "🧩", title: "保留原版能力", desc: "Mod 管理、整合包导出、版本隔离等功能完整保留" }
  ]
};

const CONTRIBUTORS = [
  {
    name: "帆蓝",
    role: "pcl-potato 二次开发者",
    desc: "更名、恢复 MC 下载、重写更新逻辑、维护更新服务器",
    avatar: "hands/fanlan.png",
    link: ""
  },
  {
    name: "龙腾猫跃",
    role: "PCL2 原作者",
    desc: "Plain Craft Launcher 的开发者",
    avatar: "hands/LTCat.jpg",
    link: ""
  },
  {
    name: "HerobrineXia",
    role: "PCL2 协力开发者",
    desc: "Plain Craft Launcher 的协力开发者",
    avatar: "hands/HerobrineXia.png",
    link: ""
  }
];

function renderIntro() {
  document.getElementById("appName").textContent = CONFIG.appName;
  document.getElementById("slogan").textContent = CONFIG.slogan;
  document.getElementById("introTitle").textContent = INTRO.title;
  document.getElementById("introText").textContent = INTRO.text;
  const box = document.getElementById("featureList");
  box.innerHTML = INTRO.features
    .map(
      f =>
        '<div class="feature"><div class="feature-icon">' +
        f.icon +
        '</div><div class="feature-title">' +
        f.title +
        '</div><div class="feature-desc">' +
        f.desc +
        "</div></div>"
    )
    .join("");
}

function renderSpecs() {
  document.getElementById("specVersion").textContent = CONFIG.version;
  document.getElementById("specCode").textContent = CONFIG.versionCode;
  document.getElementById("specSize").textContent = CONFIG.size;
  document.getElementById("specDate").textContent = CONFIG.updateDate;
  document.getElementById("specFramework").textContent = CONFIG.requireNetFramework;
}

function renderContributors() {
  const box = document.getElementById("contributorList");
  box.innerHTML = CONTRIBUTORS.map(function (c) {
    const link = c.link
      ? '<a class="contributor-link" href="' + c.link + '" target="_blank" rel="noopener">打开主页</a>'
      : "";
    return (
      '<div class="contributor">' +
      '<img class="contributor-avatar" src="' +
      c.avatar +
      '" alt="' +
      c.name +
      '">' +
      '<div class="contributor-body">' +
      '<div class="contributor-name">' +
      c.name +
      "</div>" +
      '<div class="contributor-role">' +
      c.role +
      "</div>" +
      '<div class="contributor-desc">' +
      c.desc +
      "</div>" +
      link +
      "</div></div>"
    );
  }).join("");
}

function renderDownload() {
  const btn = document.getElementById("downloadBtn");
  const sha = document.getElementById("sha256");
  const note = document.getElementById("releaseNote");

  if (CONFIG.sha256 && CONFIG.sha256.trim() !== "") {
    sha.textContent = "SHA256: " + CONFIG.sha256.trim();
    sha.style.display = "block";
  } else {
    sha.style.display = "none";
  }

  if (CONFIG.releaseState === "available" && CONFIG.downloadUrl && CONFIG.downloadUrl.trim() !== "") {
    btn.textContent = "立即下载";
    btn.className = "download-btn available";
    btn.removeAttribute("disabled");
    btn.onclick = function () {
      window.open(CONFIG.downloadUrl.trim(), "_blank");
    };
    note.textContent = "适用于 Windows 7 及以上系统，需 " + CONFIG.requireNetFramework;
  } else {
    btn.textContent = "即将发布";
    btn.className = "download-btn soon";
    btn.setAttribute("disabled", "disabled");
    btn.onclick = null;
    note.textContent = "新版本正在准备中，请稍后再来";
  }
}

function renderHero() {
  document.getElementById("heroVersion").textContent = "v" + CONFIG.version;
  document.getElementById("heroSize").textContent = CONFIG.size;
}

document.addEventListener("DOMContentLoaded", function () {
  renderHero();
  renderIntro();
  renderSpecs();
  renderContributors();
  renderDownload();
});
