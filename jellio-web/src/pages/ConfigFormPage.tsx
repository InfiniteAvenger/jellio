import type { FC } from 'react';
import ConfigForm from '@/components/configForm';
import Header from '@/components/header.tsx';
import type { ServerInfo } from '@/types';

interface Props {
  serverInfo: ServerInfo;
}

const ConfigFormPage: FC<Props> = ({ serverInfo }) => {
  const { protocol, hostname } = window.location;
  const isSecure =
    protocol === 'https:' ||
    hostname === 'localhost' ||
    hostname === '127.0.0.1';

  return (
    <div className="mx-auto max-w-2xl">
      <Header />
      {!isSecure && (
        <div className="p-4 mb-4 bg-yellow-100 text-yellow-800 rounded">
          Warning: Stremio requires addons to be served over HTTPS or from
          localhost. Please configure your Jellyfin instance with a valid SSL
          certificate or use localhost (127.0.0.1) to install this addon.
        </div>
      )}
  <ConfigForm serverInfo={serverInfo} />
    </div>
  );
};

export default ConfigFormPage;
